using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BlitzConnect.Common;
using BlitzConnect.Common.Models;
using BlitzConnect.Interactive;
using BlitzConnect.MarketData;
using Google.Protobuf;

class Program
{
    static BlitzApiClient _client = null!;
    static BlitzConfig _config = null!;
    static TestConfig _cfg = null!;
    static int _pass;
    static int _fail;
    static StreamWriter _logWriter = null!;

    static void Log(string line)
    {
        Console.WriteLine(line);
        _logWriter.WriteLine(line);
    }
    // Test SDK
    static async Task Main(string[] args)
    {
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var logPath = Path.Combine(rootDir, $"blitz-test-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        using var logWriter = new StreamWriter(logPath, append: false, encoding: Encoding.UTF8) { AutoFlush = true };
        _logWriter = logWriter;

        var jsonPath = Path.Combine(rootDir, "test-config.json");
        var jsonText = File.ReadAllText(jsonPath);
        _cfg = JsonSerializer.Deserialize<TestConfig>(jsonText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
               ?? throw new Exception("Failed to parse test-config.json");

        var envPath = Path.GetFullPath(Path.Combine(rootDir, ".env"));
        var envVars = new Dictionary<string, string>();
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;
                var eq = trimmed.IndexOf('=');
                if (eq > 0)
                    envVars[trimmed[..eq].Trim()] = trimmed[(eq + 1)..].Trim();
            }
        }

        string Env(string key, string fallback) => envVars.GetValueOrDefault(key, fallback);

        var conn = _cfg.Connection;
        var config = new BlitzConfig
        {
            MarketDataApiUrl = Env("MD_API_URL", conn.MarketDataApiUrl ?? ""),
            AuthBaseUrl = Env("AUTH_BASE_URL", conn.AuthBaseUrl ?? ""),
            OrderBaseUrl = Env("ORDER_BASE_URL", conn.OrderBaseUrl ?? ""),
            InteractiveWsUrl = Env("INTERACTIVE_WS_URL", conn.InteractiveWsUrl ?? ""),
            AppKey = Env("APP_KEY", conn.AppKey ?? ""),
            UserId = Env("USER_ID", conn.UserId ?? ""),
            ClientId = Env("CLIENT_ID", conn.ClientId ?? ""),
        };

        using var client = new BlitzApiClient(config);
        _client = client;
        _config = config;

        Log($"Log file: {logPath}");
        Log("╔══════════════════════════════════════════════╗");
        Log("║     BlitzConnect API Test Suite              ║");
        Log("╚══════════════════════════════════════════════╝");
        Log($"  MD API: {config.MarketDataApiUrl}");
        Log($"  Order API: {config.OrderBaseUrl}");
        Log($"  AppKey: {config.AppKey[..Math.Min(20, config.AppKey.Length)]}...");
        Log($"  UserId: {config.UserId}");
        Log(string.Empty);

        Log("── Authentication ──────────────────────────────");
        await _client.LoginAsync();
        Log("       login OK");

        Log(string.Empty);
        Log("── Instrument Details ──────────────────────────");
        TestAsync("GetInstrumentDetails.ById", TestGetInstrumentDetailsById);
        TestAsync("GetInstrumentDetails.BySymbol", TestGetInstrumentDetailsBySymbol);
        TestAsync("GetInstruments", TestGetInstruments);

        //Log(string.Empty);
        //Log("── Market Data ──────────────────────────────────");
        //TestAsync("GetLTP.ByIds", TestGetLtpByIds);
        //TestAsync("GetLTP.ByNames", TestGetLtpByNames);
        //TestAsync("GetOptionChain", TestGetOptionChain);
        //TestAsync("GetNiftyAtmStraddle", TestGetNiftyAtmStraddle);
        //TestAsync("GetMarketQuote.ByIds", TestGetMarketQuoteByIds);
        //TestAsync("GetMarketQuote.ByNames", TestGetMarketQuoteByNames);
        //TestAsync("GetHistoricalData", TestGetHistoricalData);

        Log(string.Empty);
        Log("── WebSocket ────────────────────────────────────");
        //TestAsync("InteractiveWS", TestWebSocket);
        //TestAsync("InteractiveWS.Client", TestWebSocketClient);

        //Log(string.Empty);
        //Log("── Trading ──────────────────────────────────────");
        //TestAsync("GetOrders", TestGetOrders);
        //TestAsync("GetOpenOrders", TestGetOpenOrders);
        //TestAsync("GetPositions", TestGetPositions);
        //TestAsync("GetTrades", TestGetTrades);
        //TestAsync("GetOrderById", TestGetOrderById);
        //TestAsync("GetTradeById", TestGetTradeById);
        //TestAsync("PlaceAndCancelCycle", TestPlaceAndCancelOrderCycle);
        //TestAsync("PlaceAndModifyCycle", TestPlaceAndModifyOrderCycle);
        //TestAsync("SendSignals", TestSendSignals);

        Log(string.Empty);
        Log("── Market Data WebSocket ────────────────────────");
        TestAsync("MarketDataWS", TestMarketDataWebSocket);

        Log(string.Empty);
        Log("══════════════════════════════════════════════════");
        Log($"RESULTS: {_pass} passed, {_fail} failed");
        Log("══════════════════════════════════════════════════");
    }

    static void Test(string name, Action action)
    {
        try { action(); Log($"  [PASS] {name}"); Interlocked.Increment(ref _pass); }
        catch (Exception ex) { Log($"  [FAIL] {name}: {ex.GetType().Name}: {ex.Message}"); Interlocked.Increment(ref _fail); }
    }

    static void TestAsync(string name, Func<Task> action) =>
        Test(name, () => action().GetAwaiter().GetResult());

    static async Task TestLogin()
    {
        await _client.LoginAsync();
        Log("       login OK");
    }

    static async Task TestGetInstrumentDetailsById()
    {
        var id = _cfg.Instruments.Select(i => i.Id).FirstOrDefault();
        var result = await _client.GetInstrumentDetailsAsync(id);
        Log($"       status={result.Status} id={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
    }

    static async Task TestGetInstrumentDetailsBySymbol()
    {
        var symbol = _cfg.Instruments.Select(i => i.Symbol).FirstOrDefault();
        var result = await _client.GetInstrumentDetailsAsync(symbol);
        Log($"       status={result.Status} id={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
    }

    static async Task TestGetInstruments()
    {
        var result = await _client.GetInstrumentsAsync();
        Log($"       status={result.Status} count={result.Data?.Count}");
        if (result.Data?.Count > 0)
        {
            var first = result.Data[0];
            Log($"       first: id={first.InstrumentId} symbol={first.Symbol}");
        }
    }

    static async Task TestGetLtpByIds()
    {
        var ids = _cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        var result = await _client.GetLtpAsync(ids);
        Log($"       status={result.Status} keys: {string.Join(", ", result.Data?.Keys ?? Enumerable.Empty<string>())}");
        if (result.Data != null)
            foreach (var (k, v) in result.Data)
                Log($"       {k}: LTP={v.Ltp}");
    }

    static async Task TestGetLtpByNames()
    {
        var ids = _cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        var result = await _client.GetLtpAsync(ids);
        Log($"       status={result.Status} keys: {string.Join(", ", result.Data?.Keys ?? Enumerable.Empty<string>())}");
        if (result.Data != null)
            foreach (var (k, v) in result.Data)
                Log($"       {k}: LTP={v.Ltp}");
    }

    static async Task TestGetOptionChain()
    {
        var md = _cfg.MarketData;
        var body = new
        {
            symbol = md.OptionChainSymbol,
            expiryDate = md.OptionChainExpiry,
            exchangeSegment = md.OptionChainExchangeSegment,
            instrumentId = md.OptionChainInstrumentId,
        };
        var result = await _client.GetOptionChainRawAsync(body);
        if (result == null)
        {
            foreach (var fallback in md.OptionChainFallbackSymbols)
            {
                var fbBody = new { symbol = fallback, expiryDate = md.OptionChainExpiry, exchangeSegment = md.OptionChainExchangeSegment, instrumentId = md.OptionChainInstrumentId };
                result = await _client.GetOptionChainRawAsync(fbBody);
                if (result != null) break;
            }
        }
        Log($"       spot={result?.Data?.SpotPrice} expiry={result?.Data?.ExpiryDate} chains={result?.Data?.Chains.Count}");
        if (result?.Data?.Chains.Count > 0)
        {
            var first = result.Data.Chains[0];
            Log($"       strike={first.StrikePrice} callLTP={first.CallOption?.Ltp} putLTP={first.PutOption?.Ltp}");
        }
    }

    static async Task TestGetNiftyAtmStraddle()
    {
        var md = _cfg.MarketData;
        var body = new { symbol = "NIFTY", expiryDate = md.RelianceOptionChainExpiry, exchangeSegment = md.OptionChainExchangeSegment, instrumentId = md.OptionChainInstrumentId };
        var result = await _client.GetOptionChainRawAsync(body);
        Log($"       Spot={result?.Data?.SpotPrice} Expiry={result?.Data?.ExpiryDate} ATM={result?.Data?.Atm}");
        var chains = result?.Data?.Chains ?? [];
        var atmEntry = chains.FirstOrDefault(c => Math.Abs(c.StrikePrice - (result?.Data?.Atm ?? 0)) < 0.01);
        if (atmEntry == null) { Log("       No ATM entry found"); return; }
        var callP = atmEntry.CallOption?.Price ?? atmEntry.CallOption?.Ltp ?? 0;
        var putP  = atmEntry.PutOption?.Price  ?? atmEntry.PutOption?.Ltp  ?? 0;
        Log($"       Strike={atmEntry.StrikePrice} Call={callP} Put={putP} Straddle={callP + putP}");
    }

    static async Task TestGetMarketQuoteByIds()
    {
        var ids = _cfg.Instruments.Select(i => i.Id).ToList();
        var result = await _client.GetMarketQuoteAsync(ids);
        Log($"       status={result.Status} entries={result.Data?.Count}");
        if (result.Data != null)
            foreach (var (k, v) in result.Data.Take(3))
                Log($"       {k}: LTP={v.Ltp} OI={v.Oi} Vol={v.Vtt}");
    }

    static async Task TestGetMarketQuoteByNames()
    {
        var ids = _cfg.Instruments.Select(i => i.Id).ToList();
        var result = await _client.GetMarketQuoteAsync(ids);
        Log($"       status={result.Status} entries={result.Data?.Count}");
    }

    static async Task TestGetHistoricalData()
    {
        var md = _cfg.MarketData;
        var result = await _client.GetHistoricalDataAsync(md.HistoricalSymbol, md.HistoricalInterval);
        Log($"       got {result.Count} candles");
        if (result.Count > 0)
        {
            var last = result[^1];
            Log($"       latest: O={last.Open} H={last.High} L={last.Low} C={last.Close} V={last.Volume} @ {last.Timestamp}");
        }
    }

    static async Task TestGetOrders()
    {
        var result = await _client.GetOrdersAsync();
        Log($"       count={result.Count}");
        foreach (var o in result.Data.Take(3))
            Log($"       OrderID={o.BlitzOrderId}");
    }

    static async Task TestGetOpenOrders()
    {
        var result = await _client.GetOpenOrdersAsync();
        Log($"       count={result.Count}");
    }

    static async Task TestGetPositions()
    {
        var result = await _client.GetPositionsAsync();
        Log($"       count={result.Count}");
    }

    static async Task TestGetTrades()
    {
        var result = await _client.GetTradesAsync();
        Log($"       count={result.Count}");
    }

    static async Task TestGetOrderById()
    {
        var orders = await _client.GetOrdersAsync();
        var id = orders.Data.FirstOrDefault()?.BlitzOrderId ?? _cfg.CancelOrder.BlitzOrderId;
        var result = await _client.GetOrderByIdAsync(id);
        Log($"       orderId={id} status={result.Status} found={result.Data != null}");
    }

    static async Task TestGetTradeById()
    {
        var trades = await _client.GetTradesAsync();
        if (trades.Count == 0) { Log("       no trades to test"); return; }
        var el = trades.Data[0];
        long tradeId = 0;
        if (el.TryGetProperty("tradeId", out var tid)) tradeId = tid.GetInt64();
        else if (el.TryGetProperty("TradeId", out var tid2)) tradeId = tid2.GetInt64();
        else if (el.TryGetProperty("blitzTradeId", out var tid3)) tradeId = tid3.GetInt64();
        else if (el.TryGetProperty("id", out var tid4)) tradeId = tid4.GetInt64();
        if (tradeId == 0) { Log("       no trade id found in response"); return; }
        var result = await _client.GetTradeByIdAsync(tradeId);
        Log($"       tradeId={tradeId} status={result.Status}");
    }

    static async Task TestPlaceAndModifyOrderCycle()
    {
        var po = _cfg.PlaceOrder;
        var ltpResp = await _client.GetLtpAsync(new List<long> { po.InstrumentId });
        var ltp = ltpResp.Data?.Values.FirstOrDefault()?.Ltp ?? po.Price;
        var placePrice = Math.Round(ltp * 0.95, 2);

        var placeResult = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = placePrice, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = po.ClientId,
        });
        Log($"       place status={placeResult.Status} message={placeResult.Message}");
        if (placeResult.Data is null) { Log("       no data, skip modify"); return; }

        var orderId = placeResult.Data.BlitzOrderId;
        Log($"       placed orderId={orderId} price={placePrice}");

        var modifyPrice = Math.Round(placePrice * 1.01, 2);
        var modifyResult = await _client.ModifyOrderAsync(new ModifyOrderRequest
        {
            BlitzOrderId = orderId, ModifiedOrderQuantity = po.Quantity, Price = modifyPrice,
            OrderType = po.OrderType, Tif = po.Tif, TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, Symbol = null,
        });
        Log($"       modify status={modifyResult.Status} message={modifyResult.Message}");
    }

    static async Task TestPlaceAndCancelOrderCycle()
    {
        var po = _cfg.PlaceOrder;
        var placeResult = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = po.Price, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = po.ClientId,
        });
        Log($"       place status={placeResult.Status} message={placeResult.Message}");
        if (placeResult.Data is null) { Log("       no data, skip cancel"); return; }

        var cancelResult = await _client.CancelOrderAsync(new CancelOrderRequest
        {
            BlitzOrderId = placeResult.Data.BlitzOrderId,
            InstrumentId = po.InstrumentId,
        });
        Log($"       cancel status={cancelResult.Status} message={cancelResult.Message}");
    }

    static async Task TestSendSignals()
    {
        var sg = _cfg.Signal;
        var baseTime = DateTime.ParseExact(sg.BaseTime, "dd-MM-yyyy HH:mm:ss", null);
        var result = await _client.SendSignalsAsync(new List<SignalRequest>
        {
            new SignalRequest
            {
                SourceStrategy = sg.SourceStrategy, DestinationStrategy = sg.DestinationStrategy,
                SourceSID = sg.SourceSID, InstanceRunningMode = sg.InstanceRunningMode,
                GlobalAction = sg.GlobalAction,
                Instruments = new List<SignalInstrument>
                {
                    new SignalInstrument
                    {
                        ExchangeSegment = sg.ExchangeSegment, InstrumentName = sg.InstrumentName,
                        Action = sg.Action, Lot = sg.Lot,
                        TimeStamp = baseTime.ToString("dd-MM-yyyy HH:mm:ss"), InfoText = sg.InfoText,
                    }
                }
            }
        });
        Log($"       status={result.Status} message={result.Message}");
    }

    static async Task TestWebSocket()
    {
        var wsUrl = _cfg.Connection.InteractiveWsUrl;
        if (string.IsNullOrEmpty(wsUrl)) throw new Exception("No InteractiveWsUrl in config");

        var token = _client.Token;
        var sep = wsUrl.Contains('?') ? '&' : '?';
        wsUrl = $"{wsUrl}{sep}access_token={token}";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
        Log("       connected");

        var buf = new byte[65536];
        var sub = Encoding.UTF8.GetBytes("{\"action\":\"AllSubscribe\"}");
        await ws.SendAsync(new ArraySegment<byte>(sub), WebSocketMessageType.Text, true, CancellationToken.None);
        Log("       subscribed");

        var po = _cfg.PlaceOrder;
        var placeResult = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = po.Price, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = _cfg.Connection.ClientId ?? "",
        });
        Log($"       placed order blitzId={placeResult.Data?.BlitzOrderId}");

        var deadline = DateTime.UtcNow.AddSeconds(15);
        bool gotMsg = false;
        while (DateTime.UtcNow < deadline && ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult res;
            try { res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (TimeoutException) { continue; }
            if (res.MessageType == WebSocketMessageType.Close) break;
            if (res.MessageType == WebSocketMessageType.Text)
            {
                gotMsg = true;
                var txt = Encoding.UTF8.GetString(buf, 0, res.Count);
                Log($"       text({res.Count}): {txt[..Math.Min(120, txt.Length)]}");
                break;
            }
            else if (res.MessageType == WebSocketMessageType.Binary)
            {
                gotMsg = true;
                Log($"       binary: {res.Count} bytes, first={BitConverter.ToString(buf, 0, Math.Min(16, res.Count))}");
                break;
            }
        }
        if (!gotMsg) Log("       no WS event (order may not trigger WS on this server)");
        else Log("       WS event received OK");

        if (placeResult.Data != null)
        {
            await _client.CancelOrderAsync(new CancelOrderRequest
            {
                BlitzOrderId = placeResult.Data.BlitzOrderId,
                InstrumentId = _cfg.PlaceOrder.InstrumentId,
            });
            Log("       order cancelled");
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        Log("       closed");
    }

    static async Task TestWebSocketClient()
    {
        var ws = new BlitzWebSocketClient(_config);
        var connected = new TaskCompletionSource<bool>();
        var gotMessage = new TaskCompletionSource<BlitzWsMessage>();
        var errors = new TaskCompletionSource<Exception>();

        ws.OnConnect += () => connected.TrySetResult(true);
        ws.OnMessage += (m) => { if (!gotMessage.Task.IsCompleted) gotMessage.TrySetResult(m); };
        ws.OnError += (e) => { if (!errors.Task.IsCompleted) errors.TrySetException(e); };

        ws.Start(_client.Token);
        Log("       starting client...");
        await connected.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Log("       connected");

        await ws.SubscribeActionAsync("AllSubscribe");
        Log("       subscribed (AllSubscribe)");

        var winner = await Task.WhenAny(gotMessage.Task, errors.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        if (winner == gotMessage.Task)
        {
            var msg = await gotMessage.Task;
            Log($"       got message type={msg.Type} code={msg.MessageCode}");
            Log("  [PASS] InteractiveWS.Client");
        }
        else if (winner == errors.Task)
        {
            Log($"       error: {errors.Task.Exception?.InnerException?.Message}");
        }
        else
        {
            Log("       no message received within 10s");
        }

        await ws.StopAsync();
        ws.Dispose();
    }

    static async Task TestMarketDataWebSocket()
    {
        var url = $"wss://uat.bull8.ai:7443/md-streaming/ws?key={_client.Token}";
        using var mdWs = new MarketDataWebSocket(url);

        int connectCount = 0;
        string? errorMsg = null;

        mdWs.OnMessage += (msg) =>
        {
            var json = Google.Protobuf.JsonFormatter.Default.Format(msg);
            Log($"New tick data received:{json}");
        };
        mdWs.OnConnected += () =>
        {
            connectCount++;
            Log($"  [WS] Connected (connect_count={connectCount})");
        };
        mdWs.OnError += (err) =>
        {
            errorMsg = err;
            Log($"  [WS] Error: {err}");
        };
        mdWs.OnDisconnected += (code, reason) => Log($"  [WS] Disconnected: code={code} reason={reason}");

        Log("  Connecting...");
        await mdWs.ConnectAsync();

        await Task.Delay(2000);

        var ids = _cfg.MarketData.InstrumentIds.Count > 0
            ? _cfg.MarketData.InstrumentIds
            : _cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        Log("  Subscribing...");
        await mdWs.SubscribeAsync(ids);
        Log($"  [PASS] Subscribed to {string.Join(", ", ids)}");

        Log("  Listening...");
        Log("  Streaming market data — press Ctrl+C to stop.");
        var shutdown = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Log("\n  Shutting down...");
            shutdown.TrySetResult();
        };
        await shutdown.Task;

        Log($"  [INFO] Connect count: {connectCount}");
        if (errorMsg != null)
            Log($"  [INFO] Errors: {errorMsg}");

        await mdWs.DisconnectAsync();
        Log("       disconnected");
        Log("  [PASS] MarketDataWS");
    }
}

// ── Config classes for test-config.json ─────────────────────────────

class TestConfig
{
    public TestConnection Connection { get; set; } = new();
    public List<TestInstrument> Instruments { get; set; } = [];
    public TestMarketData MarketData { get; set; } = new();
    public TestPlaceOrder PlaceOrder { get; set; } = new();
    public TestModifyOrder ModifyOrder { get; set; } = new();
    public TestCancelOrder CancelOrder { get; set; } = new();
    public TestSignal Signal { get; set; } = new();
}

class TestConnection
{
    public string? MarketDataApiUrl { get; set; }
    public string? AuthBaseUrl { get; set; }
    public string? OrderBaseUrl { get; set; }
    public string? InteractiveWsUrl { get; set; }
    public string? MarketDataWsUrl { get; set; }
    public string? AppKey { get; set; }
    public string? UserId { get; set; }
    public string? ClientId { get; set; }
}

class TestInstrument
{
    public long Id { get; set; }
    public string Symbol { get; set; } = "";
}

class TestMarketData
{
    public List<string> Symbols { get; set; } = [];
    public string OptionChainSymbol { get; set; } = "";
    public string OptionChainExpiry { get; set; } = "";
    public string OptionChainRequest { get; set; } = "";
    public int OptionChainExchangeSegment { get; set; }
    public long OptionChainInstrumentId { get; set; }
    public List<string> OptionChainFallbackSymbols { get; set; } = [];
    public string RelianceOptionChainSymbol { get; set; } = "";
    public string RelianceOptionChainExpiry { get; set; } = "";
    public string HistoricalSymbol { get; set; } = "";
    public string HistoricalInterval { get; set; } = "";
    public List<long> InstrumentIds { get; set; } = [];
    public int WsTimeoutSeconds { get; set; } = 15;
}

class TestPlaceOrder
{
    public int Quantity { get; set; }
    public string Product { get; set; } = "";
    public string Tif { get; set; } = "";
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    public string OrderSide { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    public long InstrumentId { get; set; }
    public string ClientId { get; set; } = "";
}

class TestModifyOrder
{
    public long BlitzOrderId { get; set; }
    public int ModifiedOrderQuantity { get; set; }
    public double Price { get; set; }
    public string OrderType { get; set; } = "";
    public string Tif { get; set; } = "";
    public int DisclosedQuantity { get; set; }
    public double StopPrice { get; set; }
    public long InstrumentId { get; set; }
    public string Symbol { get; set; } = "";
}

class TestCancelOrder
{
    public long BlitzOrderId { get; set; }
    public long InstrumentId { get; set; }
}

class TestSignal
{
    public string SourceStrategy { get; set; } = "";
    public string DestinationStrategy { get; set; } = "";
    public string SourceSID { get; set; } = "";
    public string InstanceRunningMode { get; set; } = "";
    public string GlobalAction { get; set; } = "";
    public string ExchangeSegment { get; set; } = "";
    public string InstrumentName { get; set; } = "";
    public string Action { get; set; } = "";
    public string Lot { get; set; } = "";
    public string InfoText { get; set; } = "";
    public string BaseTime { get; set; } = "";
}
