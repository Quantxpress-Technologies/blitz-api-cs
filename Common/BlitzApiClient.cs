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

    /// <summary>Raw login response body exactly as the server sent it (set after LoginAsync).</summary>
    public string? LastLoginRawResponse { get; private set; }

    /// <summary>Authenticates with the API server and stores the access token.</summary>
    public async Task LoginAsync(CancellationToken ct = default)
    {
        var url = $"{_config.AuthBaseUrl.TrimEnd('/')}/api/app_login";
        var payload = new { appKey = _config.AppKey, userId = _config.UserId };

        var resp = await _http.PostAsJsonAsync(url, payload, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        LastLoginRawResponse = text;
        var body = JsonSerializer.Deserialize<LoginResponse>(text, JsonOptions)
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

    /// <summary>Sends an HTTP request and returns the raw response body exactly as received from the server.</summary>
    protected async Task<string> RequestTextAsync(
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

                return text;
            }
        }

        throw new BlitzConnectException(0, "Request failed after all retries");
    }

    /// <summary>Sends an HTTP request and deserializes the response.</summary>
    protected async Task<T> RequestAsync<T>(
        HttpMethod method, string baseUrl, string path,
        object? body = null, string? accept = null,
        JsonSerializerOptions? serializeOptions = null,
        CancellationToken ct = default)
    {
        var text = await RequestTextAsync(method, baseUrl, path, body, accept, serializeOptions, ct);

        if (text.TrimStart().StartsWith('"'))
        {
            text = JsonSerializer.Deserialize<string>(text) ?? text;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(text, JsonOptions)
                ?? throw new BlitzConnectException(0, "Deserialization returned null");
        }
        catch (JsonException ex)
        {
            throw new BlitzConnectException(
                0, $"JSON deserialization failed for type {typeof(T).Name}", ex, text);
        }
    }

    private Task<T> MarketRequestAsync<T>(string path, object? body = null, string? accept = null, CancellationToken ct = default) =>
        RequestAsync<T>(body is null ? HttpMethod.Get : HttpMethod.Post,
            _config.MarketDataApiUrl, path, body, accept, MarketDataJsonOptions, ct);

    private Task<T> TradingRequestAsync<T>(HttpMethod method, string path, object? body = null, CancellationToken ct = default) =>
        RequestAsync<T>(method, _config.OrderBaseUrl, path, body, accept: null, ct: ct);

    /// <summary>Returns the raw market-data API response body exactly as the server sent it.</summary>
    public Task<string> MarketDataRawAsync(string path, object? body = null, string? accept = null, CancellationToken ct = default) =>
        RequestTextAsync(body is null ? HttpMethod.Get : HttpMethod.Post,
            _config.MarketDataApiUrl, path, body, accept, MarketDataJsonOptions, ct);

    /// <summary>Returns the raw interactive (trading) API response body exactly as the server sent it.</summary>
    public Task<string> TradingRawAsync(HttpMethod method, string path, object? body = null, CancellationToken ct = default) =>
        RequestTextAsync(method, _config.OrderBaseUrl, path, body, accept: null, ct: ct);

    // ── Instruments (served from the gzipped master file) ─────────────────────

    private readonly object _instrumentLock = new();
    private BlitzInstrumentManager? _instrumentManager;

    private async Task<BlitzInstrumentManager> EnsureInstrumentMasterAsync(CancellationToken ct)
    {
        if (_instrumentManager is not null) return _instrumentManager;

        if (string.IsNullOrWhiteSpace(_config.InstrumentGzUrl))
            throw new BlitzConnectException(0, "InstrumentGzUrl is not configured");

        lock (_instrumentLock)
        {
            if (_instrumentManager is not null) return _instrumentManager;
        }

        var manager = new BlitzInstrumentManager();
        await manager.LoadInstrumentsAsync(_config.InstrumentGzUrl, _token, ct);

        lock (_instrumentLock)
        {
            _instrumentManager ??= manager;
        }
        return _instrumentManager;
    }

    public async Task<BlitzApiResponse<List<InstrumentDetail>>> GetInstrumentsAsync(CancellationToken ct = default)
    {
        var manager = await EnsureInstrumentMasterAsync(ct);
        return new BlitzApiResponse<List<InstrumentDetail>> { Status = "success", Data = manager.GetAll().ToList() };
    }

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id, CancellationToken ct = default)
    {
        var manager = await EnsureInstrumentMasterAsync(ct);
        var detail = manager.GetById(id);
        return detail is null
            ? new BlitzApiResponse<InstrumentDetail> { Status = "error", Message = $"Instrument not found: {id}" }
            : new BlitzApiResponse<InstrumentDetail> { Status = "success", Data = detail };
    }

    public async Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol, CancellationToken ct = default)
    {
        var manager = await EnsureInstrumentMasterAsync(ct);
        var detail = manager.GetBySymbol(symbol);
        return detail is null
            ? new BlitzApiResponse<InstrumentDetail> { Status = "error", Message = $"Instrument not found: {symbol}" }
            : new BlitzApiResponse<InstrumentDetail> { Status = "success", Data = detail };
    }

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
        if (el.ValueKind == JsonValueKind.Object)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<Position>>>(el.GetRawText(), JsonOptions)
                       ?? new Dictionary<string, List<Position>>();
            return new PositionsResponse { Data = dict };
        }
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

    public async Task<StrategyStatisticsResponse> GetStatisticsAsync(CancellationToken ct = default)
    {
        var el = await TradingRequestAsync<JsonElement>(HttpMethod.Get, "strategy/statistics", ct: ct);
        return el.ValueKind == JsonValueKind.Array
            ? new StrategyStatisticsResponse { Data = JsonSerializer.Deserialize<List<StrategyStatistics>>(el.GetRawText(), JsonOptions) ?? [] }
            : new StrategyStatisticsResponse();
    }

    public async Task<StrategyInstanceStatisticsResponse> GetStatisticsByInstanceAsync(
        string strategyName, string strategyInstanceName, CancellationToken ct = default)
    {
        var query = new List<string>
        {
            $"strategyName={Uri.EscapeDataString(strategyName)}",
            $"strategyInstanceName={Uri.EscapeDataString(strategyInstanceName)}"
        };

        var el = await TradingRequestAsync<JsonElement>(
            HttpMethod.Get, $"strategy/statistics/instance?{string.Join("&", query)}", null, ct);

        if (el.ValueKind == JsonValueKind.Object)
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, List<StrategyStatistics>>>(el.GetRawText(), JsonOptions)
                       ?? new Dictionary<string, List<StrategyStatistics>>();
            return new StrategyInstanceStatisticsResponse { Data = dict };
        }

        return new StrategyInstanceStatisticsResponse();
    }

    // ── Write operations ─────────────────────────────────────────────────────

    public async Task<BlitzApiResponse<PlaceOrderData>> PlaceOrderAsync(PlaceOrderRequest order, CancellationToken ct = default) =>
        await TradingRequestAsync<BlitzApiResponse<PlaceOrderData>>(
            HttpMethod.Post, "orders/placeOrder", order, ct);

    public async Task<ModifyOrderResponse> ModifyOrderAsync(ModifyOrderRequest order, CancellationToken ct = default) =>
        await TradingRequestAsync<ModifyOrderResponse>(
            HttpMethod.Put, "orders/modifyOrder", order, ct);

    public async Task<GatewayResponse> CancelOrderAsync(CancelOrderRequest cancel, CancellationToken ct = default)
    {
        var query = new List<string> { $"blitzOrderId={cancel.BlitzOrderId}" };
        if (cancel.InstrumentId.HasValue)
            query.Add($"instrumentId={cancel.InstrumentId.Value}");
        if (!string.IsNullOrWhiteSpace(cancel.Symbol))
            query.Add($"symbol={Uri.EscapeDataString(cancel.Symbol)}");

        return await TradingRequestAsync<GatewayResponse>(
            HttpMethod.Delete, $"orders/cancelOrder?{string.Join("&", query)}", null, ct);
    }

    public async Task<GatewayResponse> SendSignalsAsync(List<SignalRequest> signals, CancellationToken ct = default) =>
        await TradingRequestAsync<GatewayResponse>(HttpMethod.Post, "signals", signals, ct);
}
