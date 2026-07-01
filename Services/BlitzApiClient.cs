using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlitzConnect.Models;

namespace BlitzConnect.Services;

public class BlitzApiClient : IBlitzApiClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly BlitzConfig _config;
    private string? _token;
    private readonly object _authLock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public BlitzApiClient(BlitzConfig config)
    {
        _config = config;
        _http = new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
        });
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    public async Task LoginAsync(CancellationToken ct = default)
    {
        var url = $"{_config.AuthBaseUrl.TrimEnd('/')}/api/app_login";
        var payload = new { appKey = _config.AppKey, userId = _config.UserId };

        var resp = await _http.PostAsJsonAsync(url, payload, ct);
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct)
                   ?? throw new BlitzConnectException((int)resp.StatusCode, "Empty login response");

        if (resp.StatusCode != System.Net.HttpStatusCode.OK || body.Status != "success")
            throw new BlitzConnectException((int)resp.StatusCode, body.Message ?? "Login failed");

        _token = body.Data?.AccessToken
                 ?? throw new BlitzConnectException((int)resp.StatusCode, "No access token in response");
    }

    // ── Internal request ─────────────────────────────────────────────────────

    private async Task EnsureAuthenticatedAsync(CancellationToken ct = default)
    {
        if (_token is null) await LoginAsync(ct);
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
    }
    protected async Task<T> RequestAsync<T>(
    HttpMethod method, string baseUrl, string path,
    object? body = null, string? accept = null,
    CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var url = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        Console.WriteLine($"Request: {method} {url}");

        using var req = new HttpRequestMessage(method, url);

        req.Headers.TryAddWithoutValidation("Accept", accept ?? "application/json");
        SetAuthHeader();

        string? requestBody = null;

        if (body is not null)
        {
            requestBody = JsonSerializer.Serialize(body, JsonOptions);

            Console.WriteLine("========== REQUEST ==========");
            Console.WriteLine($"Method : {method}");
            Console.WriteLine($"URL    : {url}");
            Console.WriteLine($"Body   : {requestBody}");
            Console.WriteLine("=============================");

            req.Content = new StringContent(
                requestBody,
                Encoding.UTF8,
                "application/json");
        }
        else
        {
            Console.WriteLine("========== REQUEST ==========");
            Console.WriteLine($"Method : {method}");
            Console.WriteLine($"URL    : {url}");
            Console.WriteLine("Body   : <none>");
            Console.WriteLine("=============================");
        }

        var response = await _http.SendAsync(req, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();

            lock (_authLock) { _token = null; }

            await LoginAsync(ct);
            SetAuthHeader();

            using var retryReq = new HttpRequestMessage(method, url);
            retryReq.Headers.TryAddWithoutValidation("Accept", accept ?? "application/json");

            if (body is not null)
            {
                requestBody = JsonSerializer.Serialize(body, JsonOptions);

                Console.WriteLine("========== REQUEST ==========");
                Console.WriteLine($"Method : {method}");
                Console.WriteLine($"URL    : {url}");
                Console.WriteLine($"Body   : {requestBody}");
                Console.WriteLine("=============================");

                req.Content = new StringContent(
                    requestBody,
                    Encoding.UTF8,
                    "application/json");
            }
            else
            {
                Console.WriteLine("========== REQUEST ==========");
                Console.WriteLine($"Method : {method}");
                Console.WriteLine($"URL    : {url}");
                Console.WriteLine("Body   : <none>");
                Console.WriteLine("=============================");
            }

            response = await _http.SendAsync(retryReq, ct);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(ct);

            // ================= DEBUG OUTPUT =================
            Console.WriteLine("──────── HTTP STATUS ────────");
            Console.WriteLine((int)response.StatusCode);

            Console.WriteLine("──────── RESPONSE BODY ────────");
            Console.WriteLine(text);
            Console.WriteLine("───────────────────────────────");
            // =================================================

            if (!response.IsSuccessStatusCode)
            {
                throw new BlitzConnectException(
                    (int)response.StatusCode,
                    $"Request failed ({(int)response.StatusCode}): {text}",
                    text);
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(text) &&
    text.TrimStart().StartsWith("\""))
                {
                    text = JsonSerializer.Deserialize<string>(text)!;
                }

                var result = JsonSerializer.Deserialize<T>(text, JsonOptions);

                if (result == null)
                {
                    throw new BlitzConnectException(
                        (int)response.StatusCode,
                        "Empty response after deserialization");
                }

                return result;
            }
            catch (JsonException ex)
            {
                Console.WriteLine(" JSON DESERIALIZATION ERROR");
                Console.WriteLine(text);

                throw new Exception(
                    $"JSON mismatch for type {typeof(T).Name}\nResponse:\n{text}",
                    ex);
            }
        }
    }

    private Task<T> MarketRequestAsync<T>(string path, object? body = null, string? accept = null, CancellationToken ct = default) =>
        RequestAsync<T>(body is null ? HttpMethod.Get : HttpMethod.Post,
            _config.BaseUrl, path, body, accept, ct);

    private Task<T> TradingRequestAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken ct = default) =>
        RequestAsync<T>(method, _config.OrderBaseUrl, path, body, accept: null, ct);

    // ── Market Data ──────────────────────────────────────────────────────────

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id, CancellationToken ct = default) =>
        await MarketRequestAsync<BlitzApiResponse<InstrumentDetail>>($"v1/api/instruments/{id}", ct: ct);

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol, CancellationToken ct = default) =>
        await MarketRequestAsync<BlitzApiResponse<InstrumentDetail>>($"v1/api/instruments/{symbol}", ct: ct);

    public async Task<BlitzApiResponse<List<InstrumentDetail>>> GetInstrumentsAsync(CancellationToken ct = default) =>
        await MarketRequestAsync<BlitzApiResponse<List<InstrumentDetail>>>("v1/api/instruments", ct: ct);

    public async Task<LtpResponse> GetLtpAsync(List<long> ids, CancellationToken ct = default) =>
        await MarketRequestAsync<LtpResponse>("md-api/marketfeed/ltp",
            new { instrumentIds = ids }, ct: ct);

    public async Task<LtpResponse> GetLtpAsync(List<string> names, CancellationToken ct = default) =>
        await MarketRequestAsync<LtpResponse>("md-api/marketfeed/ltp",
            new { instrumentNames = names }, ct: ct);

    public async Task<OptionChainResponse> GetOptionChainAsync(string symbol, string expiryDate, CancellationToken ct = default) =>
        await MarketRequestAsync<OptionChainResponse>("md-api/marketfeed/optionchain",
            new { symbol, expiryDate }, ct: ct);

    public async Task<OptionChainResponse?> GetOptionChainRawAsync(object body, CancellationToken ct = default)
    {
        try
        {
            return await MarketRequestAsync<OptionChainResponse>("md-api/marketfeed/optionchain", body, ct: ct);
        }
        catch (BlitzConnectException)
        {
            return null;
        }
    }

    public async Task<MarketQuoteResponse> GetMarketQuoteAsync(List<long> ids, CancellationToken ct = default) =>
        await MarketRequestAsync<MarketQuoteResponse>("md-api/marketfeed/quote",
            new { instrumentIds = ids }, ct: ct);

    public async Task<MarketQuoteResponse> GetMarketQuoteAsync(List<string> names, CancellationToken ct = default) =>
        await MarketRequestAsync<MarketQuoteResponse>("md-api/marketfeed/quote",
            new { instrumentNames = names }, ct: ct);

    public async Task<List<HistoricalDataItem>> GetHistoricalDataAsync(string instrument, string interval, CancellationToken ct = default) =>
        await MarketRequestAsync<List<HistoricalDataItem>>("md-api/marketfeed/historicalData",
            new { instrument, interval },
            accept: "*/*", ct: ct);

    public async Task<DepthResponse> GetOrderBookAsync(long instrumentId, CancellationToken ct = default) =>
        await MarketRequestAsync<DepthResponse>("md-api/marketfeed/depth",
            new { instrumentId }, ct: ct);

    // ── Trading (read-only via REST) ─────────────────────────────────────────

    public async Task<OrdersResponse> GetOrdersAsync(CancellationToken ct = default) =>
        await TradingRequestAsync<OrdersResponse>(HttpMethod.Get, "orders/history", ct: ct);

    public async Task<OrdersResponse> GetOpenOrdersAsync(CancellationToken ct = default) =>
        await TradingRequestAsync<OrdersResponse>(HttpMethod.Get, "orders/openOrders", ct: ct);

    public async Task<PositionsResponse> GetPositionsAsync(CancellationToken ct = default) =>
        await TradingRequestAsync<PositionsResponse>(HttpMethod.Get, "orders/positions", ct: ct);

    public async Task<TradesResponse> GetTradesAsync(CancellationToken ct = default) =>
        await TradingRequestAsync<TradesResponse>(HttpMethod.Get, "orders/trades", ct: ct);

    public async Task<BlitzApiResponse<OrderEntry>> GetOrderByIdAsync(long blitzOrderId, CancellationToken ct = default) =>
        await TradingRequestAsync<BlitzApiResponse<OrderEntry>>(
            HttpMethod.Get, $"orders/{blitzOrderId}", ct: ct);

    public async Task<BlitzApiResponse<object>> GetTradeByIdAsync(long tradeId, CancellationToken ct = default) =>
        await TradingRequestAsync<BlitzApiResponse<object>>(
            HttpMethod.Get, $"orders/trades/{tradeId}", ct: ct);

    // ── Write operations (will get 405 via REST) ─────────────────────────────

    public async Task<BlitzApiResponse<PlaceOrderData>> PlaceOrderAsync(PlaceOrderRequest order, CancellationToken ct = default) =>
        await TradingRequestAsync<BlitzApiResponse<PlaceOrderData>>(
            HttpMethod.Post, "orders/placeOrder", order, ct);

    public async Task<BlitzApiResponse<PlaceOrderData>> ModifyOrderAsync(ModifyOrderRequest order, CancellationToken ct = default) =>
        await TradingRequestAsync<BlitzApiResponse<PlaceOrderData>>(
            HttpMethod.Put, "orders/modifyOrder", order, ct);

    public async Task<BlitzApiResponse<object>> CancelOrderAsync(CancelOrderRequest cancel, CancellationToken ct = default)
    {
        var query = new List<string>
    {
        $"blitzOrderId={cancel.BlitzOrderId}"
    };

        if (cancel.InstrumentId.HasValue)
            query.Add($"instrumentId={cancel.InstrumentId.Value}");

        if (!string.IsNullOrWhiteSpace(cancel.Symbol))
            query.Add($"symbol={Uri.EscapeDataString(cancel.Symbol)}");

        var path = $"orders/cancelOrder?{string.Join("&", query)}";

        return await TradingRequestAsync<BlitzApiResponse<object>>(
            HttpMethod.Delete,
            path,
            null,
            ct);
    }
    public async Task<BlitzApiResponse<object>> SendSignalsAsync(List<SignalRequest> signals, CancellationToken ct = default) =>
     await TradingRequestAsync<BlitzApiResponse<object>>(
         HttpMethod.Post, "signals", signals, ct);
    public void Dispose() => _http.Dispose();

}
