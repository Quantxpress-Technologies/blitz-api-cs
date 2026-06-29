using System.Text.Json;
using BlitzConnect;
using BlitzConnect.Models;
using BlitzConnect.Services;

var envPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env");
envPath = Path.GetFullPath(envPath);

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
else
{
    Console.WriteLine($"WARNING: .env not found at {envPath}");
    Console.WriteLine("Falling back to placeholder credentials.");
}

string GetEnv(string key, string fallback) =>
    envVars.GetValueOrDefault(key, fallback);

var config = new BlitzConfig
{
    BaseUrl = GetEnv("BASE_URL", "http://uat.bull8.ai:7443"),
    AuthBaseUrl = GetEnv("AUTH_BASE_URL", "http://uat.bull8.ai:7443/api_gateway/v1"),
    OrderBaseUrl = GetEnv("ORDER_BASE_URL", "http://uat.bull8.ai:7443/api_interactive/api/v1/"),
    AppKey = GetEnv("APP_KEY", "dtuqSORnBhp3hstbJNskJFl9P2TSGozfBLjitl8VGc"),
    UserId = GetEnv("USER_ID", "Prateek123"),
    ClientId = GetEnv("CLIENT_ID", "Prateek123"),
};

using var client = new BlitzApiClient(config);
var pass = 0;
var fail = 0;

void Test(string name, Action action)
{
    try
    {
        action();
        Console.WriteLine($"  [PASS] {name}");
        Interlocked.Increment(ref pass);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  [FAIL] {name}: {ex.GetType().Name}: {ex.Message}");
        Interlocked.Increment(ref fail);
    }
}

void TestAsync(string name, Func<Task> action) =>
    Test(name, () => action().GetAwaiter().GetResult());

// ═══════════════════════════════════════════════════════════════════
//  TEST DEFINITIONS
// ═══════════════════════════════════════════════════════════════════

async Task TestLogin()
{
    await client.LoginAsync();
    Console.WriteLine("       login OK");
}

async Task TestGetInstrumentDetailsById()
{
    var result = await client.GetInstrumentDetailsAsync(110010000002885);
    Console.WriteLine($"       instrumentId={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
}

async Task TestGetInstrumentDetailsBySymbol()
{
    var result = await client.GetInstrumentDetailsAsync("NSECM:RELIANCE");
    Console.WriteLine($"       instrumentId={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
}

async Task TestGetLtpByIds()
{
    var result = await client.GetLtpAsync(new List<long> { 110010000002885, 11001000200002 });
    Console.WriteLine($"       status={result.Status} data keys: {string.Join(", ", result.Data?.Keys ?? Enumerable.Empty<string>())}");
    if (result.Data != null)
        foreach (var (key, entry) in result.Data)
            Console.WriteLine($"       {key}: LTP={entry.Ltp}");
}

async Task TestGetLtpByNames()
{
    var result = await client.GetLtpAsync(new List<string> { "NSEFO:NIFTY28APR26FUT", "NSECM:NIFTY BANK" });
    Console.WriteLine($"       status={result.Status}");
    if (result.Data != null)
        foreach (var (key, entry) in result.Data)
            Console.WriteLine($"       {key}: LTP={entry.Ltp}");
}

async Task TestGetOptionChain()
{
    var result = await client.GetOptionChainAsync("NIFTY", "2026-06-30");
    Console.WriteLine($"       spot={result.Data?.SpotPrice} expiry={result.Data?.ExpiryDate} chains={result.Data?.Chains.Count}");
    if (result.Data?.Chains.Count > 0)
    {
        var first = result.Data.Chains[0];
        Console.WriteLine($"       strike={first.StrikePrice} callLTP={first.CallOption?.Ltp} putLTP={first.PutOption?.Ltp}");
    }
}

async Task TestGetMarketQuoteByIds()
{
    var result = await client.GetMarketQuoteAsync(new List<long> { 110010000002885, 110010002000002, 110010002000001 });
    Console.WriteLine($"       status={result.Status} entries={result.Data?.Count}");
    if (result.Data != null)
        foreach (var (key, entry) in result.Data.Take(3))
            Console.WriteLine($"       {key}: LTP={entry.Ltp} OI={entry.Oi} Vol={entry.Vtt}");
}

async Task TestGetMarketQuoteByNames()
{
    var result = await client.GetMarketQuoteAsync(new List<string> { "NSECM:TCS", "NSEFO:NIFTY28APR26FUT" });
    Console.WriteLine($"       status={result.Status} entries={result.Data?.Count}");
}

async Task TestGetHistoricalData()
{
    var result = await client.GetHistoricalDataAsync("TCS", "D");
    Console.WriteLine($"       got {result.Count} daily candles");
    if (result.Count > 0)
    {
        var last = result[^1];
        Console.WriteLine($"       latest: O={last.Open} H={last.High} L={last.Low} C={last.Close} V={last.Volume} @ {last.Timestamp}");
    }
}

async Task TestGetOrders()
{
    var result = await client.GetOrdersAsync();
    Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
    foreach (var o in (result.Data ?? []).Take(3))
        Console.WriteLine($"       OrderID={o.BlitzOrderId}");
}

async Task TestGetOpenOrders()
{
    var result = await client.GetOpenOrdersAsync();
    Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
}

async Task TestGetPositions()
{
    var result = await client.GetPositionsAsync();
    Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
}

async Task TestGetTrades()
{
    var result = await client.GetTradesAsync();
    Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
}

async Task TestPlaceOrder()
{
    var result = await client.PlaceOrderAsync(new PlaceOrderRequest
    {
        CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
        Quantity = 1,
        Product = "CNC",
        Tif = "GFD",
        Price = 2945,
        OrderType = "LIMIT",
        OrderSide = "BUY",
        DisclosedQuantity = 0,
        StopPrice = 0.0,
        TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
        InstrumentId = 110010000011536,
        ClientId = "Algo123",
    });
    Console.WriteLine($"       status={result.Status} message={result.Message}");
}

async Task TestModifyOrder()
{
    var result = await client.ModifyOrderAsync(new ModifyOrderRequest
    {
        BlitzOrderId = 226111752520000015,
        ModifiedOrderQuantity = 10,
        Price = 11.01,
        OrderType = "MARKET",
        Tif = "GTD",
        DisclosedQuantity = 0,
        StopPrice = 0.0,
        TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
        InstrumentId = 110010000011536,
        Symbol = "NSECM|IDEA",
    });
    Console.WriteLine($"       status={result.Status} message={result.Message}");
}

async Task TestCancelOrder()
{
    var result = await client.CancelOrderAsync(new CancelOrderRequest
    {
        BlitzOrderId = 226111752520000020,
        InstrumentId = 110010000011536,
    });
    Console.WriteLine($"       status={result.Status} message={result.Message}");
}

async Task TestSendSignals()
{
    var baseTime = DateTime.ParseExact(
        "18-12-2025 09:21:00",
        "dd-MM-yyyy HH:mm:ss",
        null);

    var instrumentName = "NIFTY10FEB2625550PE";

    var signals = new List<SignalRequest>
    {
        new SignalRequest
        {
            SourceStrategy = "Bull8.AmberX1",
            DestinationStrategy = "Matrix",
            SourceSID = "Bull8_SINGLE_Matrix",
            InstanceRunningMode = "Started",
            GlobalAction = "Signal",
            Instruments = new List<SignalInstrument>
            {
                new SignalInstrument
                {
                    ExchangeSegment = "NSEFO",
                    InstrumentName = instrumentName,
                    Action = "BUY",
                    Lot = "27",
                    TimeStamp = baseTime.ToString("dd-MM-yyyy HH:mm:ss"),
                    InfoText = "PE Entry Signal 25550"
                }
            }
        }
    };

    var result = await client.SendSignalsAsync(signals);
    Console.WriteLine($"       status={result.Status} message={result.Message}");
}

// ═══════════════════════════════════════════════════════════════════
//  RUN
// ═══════════════════════════════════════════════════════════════════

Console.WriteLine("╔══════════════════════════════════════════════╗");
Console.WriteLine("║     BlitzConnect API Test Suite              ║");
Console.WriteLine("╚══════════════════════════════════════════════╝");
Console.WriteLine($"  BaseUrl: {config.BaseUrl}");
Console.WriteLine($"  AppKey: {config.AppKey[..Math.Min(20, config.AppKey.Length)]}...");
Console.WriteLine($"  UserId: {config.UserId}");
Console.WriteLine();

Console.WriteLine("── Authentication ──────────────────────────────");
TestAsync("Login", TestLogin);

Console.WriteLine();
Console.WriteLine("── Market Data ──────────────────────────────────");

//TestAsync("GetInstrumentDetails.ById", TestGetInstrumentDetailsById);
//TestAsync("GetInstrumentDetails.BySymbol", TestGetInstrumentDetailsBySymbol);
//TestAsync("GetLTP.ByIds", TestGetLtpByIds);
//TestAsync("GetLTP.ByNames", TestGetLtpByNames);
//TestAsync("GetOptionChain", TestGetOptionChain);
//TestAsync("GetMarketQuote.ByIds", TestGetMarketQuoteByIds);
//TestAsync("GetMarketQuote.ByNames", TestGetMarketQuoteByNames);
//TestAsync("GetHistoricalData", TestGetHistoricalData);

//Console.WriteLine();
//Console.WriteLine("── Trading ──────────────────────────────────────");

TestAsync("GetOrders", TestGetOrders);
//TestAsync("GetOpenOrders", TestGetOpenOrders);
//TestAsync("GetPositions", TestGetPositions);
//TestAsync("GetTrades", TestGetTrades);
//TestAsync("PlaceOrder", TestPlaceOrder);
//TestAsync("ModifyOrder", TestModifyOrder);
//TestAsync("CancelOrder", TestCancelOrder);

//Console.WriteLine();
//Console.WriteLine("── Signals ─────────────────────────────────────");

//TestAsync("SendSignals", TestSendSignals);

// ═══════════════════════════════════════════════════════════════════
//  SUMMARY
// ═══════════════════════════════════════════════════════════════════

Console.WriteLine();
Console.WriteLine($"╔══════════════════════════════════════════════╗");
Console.WriteLine($"║  PASSED: {pass,3}   FAILED: {fail,3}               ║");
Console.WriteLine($"╚══════════════════════════════════════════════╝");
