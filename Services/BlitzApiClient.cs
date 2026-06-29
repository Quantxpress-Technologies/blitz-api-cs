using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlitzConnect.Models;

namespace BlitzConnect.Services;

public class BlitzApiClient : IDisposable
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

    public async Task LoginAsync()
    {
        var url = $"{_config.AuthBaseUrl.TrimEnd('/')}/api/app_login";
        var payload = new { appKey = _config.AppKey, userId = _config.UserId };

        var resp = await _http.PostAsJsonAsync(url, payload);
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions)
                   ?? throw new BlitzConnectException((int)resp.StatusCode, "Empty login response");

        if (resp.StatusCode != System.Net.HttpStatusCode.OK || body.Status != "success")
            throw new BlitzConnectException((int)resp.StatusCode, body.Message ?? "Login failed");

        _token = body.Data?.AccessToken
                 ?? throw new BlitzConnectException((int)resp.StatusCode, "No access token in response");
    }

    // ── Internal request ─────────────────────────────────────────────────────

    private async Task EnsureAuthenticatedAsync()
    {
        if (_token is null) await LoginAsync();
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
    }
    protected async Task<T> RequestAsync<T>(
    HttpMethod method, string baseUrl, string path,
    object? body = null, string? accept = null)
    {
        await EnsureAuthenticatedAsync();

        var url = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";
        Console.WriteLine($"Request: {method} {url}");

        using var req = new HttpRequestMessage(method, url);

        req.Headers.TryAddWithoutValidation("Accept", accept ?? "application/json");
        SetAuthHeader();

        //if (body is not null)
        //{
        //    req.Content = new StringContent(
        //        JsonSerializer.Serialize(body, JsonOptions),
        //        Encoding.UTF8,
        //        "application/json");
        //}
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

        var response = await _http.SendAsync(req);

        var retried = false;

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized && !retried)
        {
            response.Dispose();

            lock (_authLock) { _token = null; }

            await LoginAsync();
            SetAuthHeader();

            using var retryReq = new HttpRequestMessage(method, url);
            retryReq.Headers.TryAddWithoutValidation("Accept", accept ?? "application/json");

            //if (body is not null)
            //{
            //    retryReq.Content = new StringContent(
            //        JsonSerializer.Serialize(body, JsonOptions),
            //        Encoding.UTF8,
            //        "application/json");
            //}

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

            response = await _http.SendAsync(retryReq);
            retried = true;
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync();

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
                    $"Request failed ({(int)response.StatusCode}): {text}");
            }

            try
            {
                //var result = JsonSerializer.Deserialize<T>(text, JsonOptions);
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

    private Task<T> MarketRequestAsync<T>(string path, object? body = null, string? accept = null) =>
        RequestAsync<T>(body is null ? HttpMethod.Get : HttpMethod.Post,
            _config.BaseUrl, path, body, accept);

    private Task<T> TradingRequestAsync<T>(HttpMethod method, string path, object? body = null) =>
        RequestAsync<T>(method, _config.OrderBaseUrl, path, body);

    // ── Market Data ──────────────────────────────────────────────────────────

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id) =>
        await MarketRequestAsync<BlitzApiResponse<InstrumentDetail>>($"v1/api/instruments/{id}");

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol) =>
        await MarketRequestAsync<BlitzApiResponse<InstrumentDetail>>($"v1/api/instruments/{symbol}");

    public async Task<LtpResponse> GetLtpAsync(List<long> ids) =>
        await MarketRequestAsync<LtpResponse>("md-api/marketfeed/ltp",
            new { instrumentIds = ids });

    public async Task<LtpResponse> GetLtpAsync(List<string> names) =>
        await MarketRequestAsync<LtpResponse>("md-api/marketfeed/ltp",
            new { instrumentNames = names });

    public async Task<OptionChainResponse> GetOptionChainAsync(string symbol, string expiryDate) =>
        await MarketRequestAsync<OptionChainResponse>("md-api/marketfeed/optionchain",
            new { symbol, expiryDate });

    public async Task<MarketQuoteResponse> GetMarketQuoteAsync(List<long> ids) =>
        await MarketRequestAsync<MarketQuoteResponse>("md-api/marketfeed/quote",
            new { instrumentIds = ids });

    public async Task<MarketQuoteResponse> GetMarketQuoteAsync(List<string> names) =>
        await MarketRequestAsync<MarketQuoteResponse>("md-api/marketfeed/quote",
            new { instrumentNames = names });

    public async Task<List<HistoricalDataItem>> GetHistoricalDataAsync(string instrument, string interval) =>
        await MarketRequestAsync<List<HistoricalDataItem>>("md-api/marketfeed/historicalData",
            new { instrument, interval },
            accept: "*/*");

    // ── Trading (read-only via REST) ─────────────────────────────────────────

    public async Task<OrdersResponse> GetOrdersAsync() =>
        await TradingRequestAsync<OrdersResponse>(HttpMethod.Get, "orders/history");

    public async Task<OrdersResponse> GetOpenOrdersAsync() =>
        await TradingRequestAsync<OrdersResponse>(HttpMethod.Get, "orders/openOrders");

    public async Task<PositionsResponse> GetPositionsAsync() =>
        await TradingRequestAsync<PositionsResponse>(HttpMethod.Get, "orders/positions");

    public async Task<TradesResponse> GetTradesAsync() =>
        await TradingRequestAsync<TradesResponse>(HttpMethod.Get, "orders/trades");

    // ── Write operations (will get 405 via REST) ─────────────────────────────

    public async Task<BlitzApiResponse<object>> PlaceOrderAsync(PlaceOrderRequest order) =>
        await TradingRequestAsync<BlitzApiResponse<object>>(
            HttpMethod.Post, "orders/placeOrder", order);

    public async Task<BlitzApiResponse<object>> ModifyOrderAsync(ModifyOrderRequest order) =>
        await TradingRequestAsync<BlitzApiResponse<object>>(
            HttpMethod.Put, "orders/modifyOrder", order);

    //public async Task<BlitzApiResponse<object>> CancelOrderAsync(CancelOrderRequest cancel) =>
    //    await TradingRequestAsync<BlitzApiResponse<object>>(
    //        HttpMethod.Delete, "orders/cancelOrder", cancel);

    public async Task<BlitzApiResponse<object>> CancelOrderAsync(CancelOrderRequest cancel)
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
            null);
    }
    public async Task<BlitzApiResponse<object>> SendSignalsAsync(List<SignalRequest> signals) =>
     await TradingRequestAsync<BlitzApiResponse<object>>(
         HttpMethod.Post, "signals", signals);
    public void Dispose() => _http.Dispose();

}
