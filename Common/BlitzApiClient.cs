using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlitzConnect.Common.Models;

namespace BlitzConnect.Common;

/// <summary>Core HTTP client for Blitz Interactive and Market Data APIs.</summary>
public class BlitzApiClient : IBlitzApiClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly BlitzConfig _config;
    private string? _token;
    public string? Token => _token;
    private readonly object _authLock = new();
    private bool _disposed;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // MD API expects PascalCase JSON field names
    private static readonly JsonSerializerOptions MarketDataJsonOptions = new()
    {
        PropertyNamingPolicy = null,
    };

    private const int MaxRetries = 3;
    private static readonly TimeSpan[] RetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)];

    /// <summary>Creates a new API client using the given configuration.</summary>
    public BlitzApiClient(BlitzConfig config)
    {
        _config = config;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            MaxConnectionsPerServer = 10,
        };
        _http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds) };
        _http.DefaultRequestHeaders.ExpectContinue = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
    }

    /// <summary>Authenticates with the API server and stores the access token.</summary>
    public async Task LoginAsync(CancellationToken ct = default)
    {
        var url = $"{_config.AuthBaseUrl.TrimEnd('/')}/api/app_login";
        var payload = new { appKey = _config.AppKey, userId = _config.UserId };

        var resp = await _http.PostAsJsonAsync(url, payload, ct);
        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, ct)
                   ?? throw new BlitzConnectException((int)resp.StatusCode, "Empty login response");

        if (resp.StatusCode != HttpStatusCode.OK || body.Status != "success")
            throw new BlitzConnectException((int)resp.StatusCode, body.Message ?? "Login failed");

        _token = body.Data?.AccessToken
                 ?? throw new BlitzConnectException((int)resp.StatusCode, "No access token in response");
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken ct)
    {
        if (_token is null) await LoginAsync(ct);
    }

    private void SetAuthHeader()
    {
        _http.DefaultRequestHeaders.Remove("Authorization");
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_token}");
    }

    /// <summary>Sends an HTTP request and deserializes the response.</summary>
    protected async Task<T> RequestAsync<T>(
        HttpMethod method, string baseUrl, string path,
        object? body = null, string? accept = null,
        JsonSerializerOptions? serializeOptions = null,
        CancellationToken ct = default)
    {
        await EnsureAuthenticatedAsync(ct);

        var url = $"{baseUrl.TrimEnd('/')}/{path.TrimStart('/')}";

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            using var req = new HttpRequestMessage(method, url);
            req.Headers.TryAddWithoutValidation("Accept", accept ?? "application/json");
            SetAuthHeader();

            if (body is not null)
            {
                var json = JsonSerializer.Serialize(body, serializeOptions ?? JsonOptions);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }

            HttpResponseMessage response;
            try
            {
                response = await _http.SendAsync(req, ct);
            }
            catch (TaskCanceledException) when (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelays[attempt], ct);
                continue;
            }
            catch (HttpRequestException) when (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelays[attempt], ct);
                continue;
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    lock (_authLock) { _token = null; }
                    await LoginAsync(ct);
                    SetAuthHeader();
                    continue;
                }

                var text = await response.Content.ReadAsStringAsync(ct);

                if (!response.IsSuccessStatusCode)
                {
                    throw new BlitzConnectException(
                        (int)response.StatusCode,
                        $"Request failed ({(int)response.StatusCode}): {text}",
                        text);
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new BlitzConnectException(
                        (int)response.StatusCode, "Empty response from server");
                }

                if (text.TrimStart().StartsWith('"'))
                {
                    text = JsonSerializer.Deserialize<string>(text) ?? text;
                }

                try
                {
                    return JsonSerializer.Deserialize<T>(text, JsonOptions)
                        ?? throw new BlitzConnectException(
                            (int)response.StatusCode, "Deserialization returned null");
                }
                catch (JsonException ex)
                {
                    throw new BlitzConnectException(
                        (int)response.StatusCode,
                        $"JSON deserialization failed for type {typeof(T).Name}",
                        ex, text);
                }
            }
        }

        throw new BlitzConnectException(0, "Request failed after all retries");
    }

    private string InstrumentBaseUrl =>
        !string.IsNullOrEmpty(_config.InstrumentGzUrl)
            ? new Uri(_config.InstrumentGzUrl).GetLeftPart(UriPartial.Authority)
            : _config.MarketDataApiUrl;

    private Task<T> MarketRequestAsync<T>(string path, object? body = null, string? accept = null, CancellationToken ct = default) =>
        RequestAsync<T>(body is null ? HttpMethod.Get : HttpMethod.Post,
            _config.MarketDataApiUrl, path, body, accept, MarketDataJsonOptions, ct);

    private Task<T> InstrumentRequestAsync<T>(string path, CancellationToken ct = default) =>
        RequestAsync<T>(HttpMethod.Get, InstrumentBaseUrl, path, ct: ct);

    private Task<T> TradingRequestAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken ct = default) =>
        RequestAsync<T>(method, _config.OrderBaseUrl, path, body, accept: null, ct: ct);

    // ── Market Data ──────────────────────────────────────────────────────────

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id, CancellationToken ct = default) =>
        await InstrumentRequestAsync<BlitzApiResponse<InstrumentDetail>>($"v1/api/instruments/{id}", ct: ct);

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol, CancellationToken ct = default) =>
        await InstrumentRequestAsync<BlitzApiResponse<InstrumentDetail>>($"v1/api/instruments/{symbol}", ct: ct);

    public async Task<BlitzApiResponse<List<InstrumentDetail>>> GetInstrumentsAsync(CancellationToken ct = default) =>
        await InstrumentRequestAsync<BlitzApiResponse<List<InstrumentDetail>>>("v1/api/instruments", ct: ct);

    public async Task<LtpResponse> GetLtpAsync(List<long> ids, CancellationToken ct = default) =>
        await MarketRequestAsync<LtpResponse>("marketfeed/ltp", new { InstrumentIds = ids }, ct: ct);

    public async Task<OptionChainResponse> GetOptionChainAsync(string symbol, string expiryDate, CancellationToken ct = default) =>
        await MarketRequestAsync<OptionChainResponse>("marketfeed/optionChain",
            new { Symbol = symbol, ExpiryDate = expiryDate }, ct: ct);

    public async Task<OptionChainResponse?> GetOptionChainRawAsync(object body, CancellationToken ct = default)
    {
        try { return await MarketRequestAsync<OptionChainResponse>("marketfeed/optionchain", body, ct: ct); }
        catch (BlitzConnectException) { return null; }
    }

    public async Task<MarketQuoteResponse> GetMarketQuoteAsync(List<long> ids, CancellationToken ct = default) =>
        await MarketRequestAsync<MarketQuoteResponse>("marketfeed/quote",
            new { InstrumentIds = ids }, ct: ct);

    public async Task<List<HistoricalDataItem>> GetHistoricalDataAsync(string instrument, string interval, CancellationToken ct = default) =>
        await MarketRequestAsync<List<HistoricalDataItem>>("marketfeed/historicalData",
            new { Instrument = instrument, interval }, accept: "*/*", ct: ct);

    // ── Trading (read-only) ──────────────────────────────────────────────────

    public async Task<OrdersResponse> GetOrdersAsync(CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, "orders", ct: ct);
        return el.ValueKind == JsonValueKind.Array
            ? new OrdersResponse { Data = JsonSerializer.Deserialize<List<OrderEntry>>(el.GetRawText(), JsonOptions) ?? [] }
            : new OrdersResponse();
    }

    public async Task<OrdersResponse> GetOpenOrdersAsync(CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, "orders/openOrders", ct: ct);
        return el.ValueKind == JsonValueKind.Array
            ? new OrdersResponse { Data = JsonSerializer.Deserialize<List<OrderEntry>>(el.GetRawText(), JsonOptions) ?? [] }
            : new OrdersResponse();
    }

    public async Task<PositionsResponse> GetPositionsAsync(CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, "positions", ct: ct);
        return new PositionsResponse();
    }

    public async Task<TradesResponse> GetTradesAsync(CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, "trades", ct: ct);
        return el.ValueKind == JsonValueKind.Array
            ? new TradesResponse { Data = JsonSerializer.Deserialize<List<JsonElement>>(el.GetRawText(), JsonOptions) ?? [] }
            : new TradesResponse();
    }

    public async Task<BlitzApiResponse<OrderEntry>> GetOrderByIdAsync(long blitzOrderId, CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, $"orders/{blitzOrderId}", ct: ct);
        var entry = el.ValueKind == JsonValueKind.Object
            ? JsonSerializer.Deserialize<OrderEntry>(el.GetRawText(), JsonOptions)
            : null;
        return new BlitzApiResponse<OrderEntry> { Status = "success", Data = entry };
    }

    public async Task<BlitzApiResponse<object>> GetTradeByIdAsync(long tradeId, CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, $"orders/trades/{tradeId}", ct: ct);
        return new BlitzApiResponse<object> { Status = "success" };
    }

    // ── Write operations ─────────────────────────────────────────────────────

    public async Task<BlitzApiResponse<PlaceOrderData>> PlaceOrderAsync(PlaceOrderRequest order, CancellationToken ct = default) =>
        await TradingRequestAsync<BlitzApiResponse<PlaceOrderData>>(
            HttpMethod.Post, "orders/placeOrder", order, ct);

    public async Task<BlitzApiResponse<PlaceOrderData>> ModifyOrderAsync(ModifyOrderRequest order, CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Put, "orders/modifyOrder", order, ct: ct);
        var text = el.GetRawText();
        try
        {
            return JsonSerializer.Deserialize<BlitzApiResponse<PlaceOrderData>>(text, JsonOptions)
                   ?? new BlitzApiResponse<PlaceOrderData> { Status = "success" };
        }
        catch (JsonException)
        {
            return new BlitzApiResponse<PlaceOrderData> { Status = text };
        }
    }

    public async Task<BlitzApiResponse<object>> CancelOrderAsync(CancelOrderRequest cancel, CancellationToken ct = default)
    {
        var query = new List<string> { $"blitzOrderId={cancel.BlitzOrderId}" };
        if (cancel.InstrumentId.HasValue)
            query.Add($"instrumentId={cancel.InstrumentId.Value}");
        if (!string.IsNullOrWhiteSpace(cancel.Symbol))
            query.Add($"symbol={Uri.EscapeDataString(cancel.Symbol)}");

        var el = await TradingRequestAsync<JsonElement>(
            HttpMethod.Delete, $"orders/cancelOrder?{string.Join("&", query)}", null, ct);
        return new BlitzApiResponse<object> { Status = "success", Data = el.GetRawText() };
    }

    public async Task<BlitzApiResponse<object>> SendSignalsAsync(List<SignalRequest> signals, CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Post, "signals", signals, ct);
        return new BlitzApiResponse<object> { Status = "success", Data = el.GetRawText() };
    }
}
