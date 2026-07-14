using System.Xml.Linq;
using BlitzConnect;
using BlitzConnect.Models;
using BlitzConnect.Services;

class Program
{
    static BlitzApiClient _client = null!;
    static XDocument _xml = null!;
    static int _pass;
    static int _fail;

    static string Xml(string path)
    {
        var parts = path.Split('/');
        var el = _xml.Root!.Element(parts[0]);
        for (int i = 1; el != null && i < parts.Length; i++)
            el = el.Element(parts[i]);
        return el?.Value ?? "";
    }

    static string XmlAttr(string element, string attr) =>
        _xml.Root!.Element(element)?.Attribute(attr)?.Value ?? "";

    static string[] XmlList(string parent, string child)
    {
        var parentEl = _xml.Root!.Element(parent);
        return parentEl != null
            ? parentEl.Elements(child).Select(e => e.Value).ToArray()
            : [];
    }

    static long XmlLong(string path) => long.Parse(Xml(path));
    static long XmlLongAttr(string element, string attr) => long.Parse(XmlAttr(element, attr));
    static double XmlDouble(string path) => double.Parse(Xml(path));
    static int XmlInt(string path) => int.Parse(Xml(path));

    static async Task Main(string[] args)
    {
        var rootDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

        var xmlPath = Path.Combine(rootDir, "BlitzConnectTest", "test-config.xml");
        if (File.Exists(xmlPath))
            _xml = XDocument.Load(xmlPath);
        else
            Console.WriteLine($"WARNING: test-config.xml not found at {xmlPath}");

        var envPath = Path.Combine(rootDir, ".env");
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
        }

        string GetEnv(string key, string fallback) =>
            envVars.GetValueOrDefault(key, fallback);

        var config = new BlitzConfig
        {
            BaseUrl = GetEnv("BASE_URL", Xml("Connection/BaseUrl")),
            AuthBaseUrl = GetEnv("AUTH_BASE_URL", Xml("Connection/AuthBaseUrl")),
            OrderBaseUrl = GetEnv("ORDER_BASE_URL", Xml("Connection/OrderBaseUrl")),
            AppKey = GetEnv("APP_KEY", Xml("Connection/AppKey")),
            UserId = GetEnv("USER_ID", Xml("Connection/UserId")),
            ClientId = GetEnv("CLIENT_ID", Xml("Connection/ClientId")),
        };

        using var client = new BlitzApiClient(config);
        _client = client;

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
        //TestAsync("GetInstruments", TestGetInstruments);
        TestAsync("GetLTP.ByIds", TestGetLtpByIds);
        TestAsync("GetLTP.ByNames", TestGetLtpByNames);
        TestAsync("GetOptionChain", TestGetOptionChain);
        TestAsync("GetRelianceAtmStraddle", TestGetRelianceAtmStraddle);
        TestAsync("GetMarketQuote.ByIds", TestGetMarketQuoteByIds);
        TestAsync("GetMarketQuote.ByNames", TestGetMarketQuoteByNames);
        TestAsync("GetHistoricalData", TestGetHistoricalData);
        TestAsync("GetOrderBook", TestGetOrderBook);

        Console.WriteLine();
        Console.WriteLine("── Trading ──────────────────────────────────────");

        TestAsync("GetOrders", TestGetOrders);
        TestAsync("GetOpenOrders", TestGetOpenOrders);
        TestAsync("GetPositions", TestGetPositions);
        TestAsync("GetTrades", TestGetTrades);
        TestAsync("PlaceAndCancelCycle", TestPlaceAndCancelOrderCycle);
        TestAsync("PlaceAndModifyCycle", TestPlaceAndModifyOrderCycle);
        //TestAsync("ModifyOrder", TestModifyOrder);
        //TestAsync("CancelOrder", TestCancelOrder);

        //Console.WriteLine();
        //Console.WriteLine("── Signals ─────────────────────────────────────");

        TestAsync("SendSignals", TestSendSignals);

        Console.WriteLine();
        Console.WriteLine($"╔══════════════════════════════════════════════╗");
        Console.WriteLine($"║  PASSED: {_pass,3}   FAILED: {_fail,3}               ║");
        Console.WriteLine($"╚══════════════════════════════════════════════╝");
    }

    // ── Test runner helpers ──────────────────────────────────────────

    static void Test(string name, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"  [PASS] {name}");
            Interlocked.Increment(ref _pass);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] {name}: {ex.GetType().Name}: {ex.Message}");
            Interlocked.Increment(ref _fail);
        }
    }

    static void TestAsync(string name, Func<Task> action) =>
        Test(name, () => action().GetAwaiter().GetResult());

    // ═══════════════════════════════════════════════════════════════════
    //  TEST DEFINITIONS
    // ═══════════════════════════════════════════════════════════════════

    static async Task TestLogin()
    {
        await _client.LoginAsync();
        Console.WriteLine("       login OK");
    }

    static async Task TestGetInstrumentDetailsById()
    {
        var id = XmlLongAttr("Instrument", "Id");
        var result = await _client.GetInstrumentDetailsAsync(id);
        Console.WriteLine($"       instrumentId={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
    }

    static async Task TestGetInstrumentDetailsBySymbol()
    {
        var symbol = Xml("MarketData/Symbol");
        var result = await _client.GetInstrumentDetailsAsync(symbol);
        Console.WriteLine($"       instrumentId={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
    }

    static async Task TestGetLtpByIds()
    {
        var ids = _xml.Root!.Elements("Instrument").Select(e => long.Parse(e.Attribute("Id")!.Value)).Take(2).ToList();
        var result = await _client.GetLtpAsync(ids);
        Console.WriteLine($"       status={result.Status} data keys: {string.Join(", ", result.Data?.Keys ?? Enumerable.Empty<string>())}");
        if (result.Data != null)
            foreach (var (key, entry) in result.Data)
                Console.WriteLine($"       {key}: LTP={entry.Ltp}");
    }

    static async Task TestGetLtpByNames()
    {
        var names = XmlList("MarketData", "Symbol").Take(2).ToList();
        var result = await _client.GetLtpAsync(names);
        Console.WriteLine($"       status={result.Status}");
        if (result.Data != null)
            foreach (var (key, entry) in result.Data)
                Console.WriteLine($"       {key}: LTP={entry.Ltp}");
    }

    static async Task TestGetOptionChain()
    {
        var symbol = Xml("MarketData/OptionChainSymbol");
        var expiry = Xml("MarketData/OptionChainExpiry");
        var result = await _client.GetOptionChainAsync(symbol, expiry);
        Console.WriteLine($"       spot={result.Data?.SpotPrice} expiry={result.Data?.ExpiryDate} chains={result.Data?.Chains.Count}");
        if (result.Data?.Chains.Count > 0)
        {
            var first = result.Data.Chains[0];
            Console.WriteLine($"       strike={first.StrikePrice} callLTP={first.CallOption?.Ltp} putLTP={first.PutOption?.Ltp}");
        }
    }

    static async Task TestGetRelianceAtmStraddle()
    {
        var symbol = Xml("MarketData/RelianceOptionChainSymbol");
        var expiry = Xml("MarketData/RelianceOptionChainExpiry");

        OptionChainResponse? result = null;

        // Try 1: instrument ID as number
        Console.WriteLine("       Trying instrumentId (number) as symbol...");
        result = await _client.GetOptionChainRawAsync(
            new { symbol = 110010000002885L, expiryDate = expiry });

        // Try 2: with exchangeSegment
        if (result == null)
        {
            Console.WriteLine("       Trying with exchangeSegment...");
            result = await _client.GetOptionChainRawAsync(
                new { symbol, expiryDate = expiry, exchangeSegment = 2 });
        }

        // Try 3: instrument ID as string
        if (result == null)
        {
            Console.WriteLine("       Trying instrumentId (string) as symbol...");
            result = await _client.GetOptionChainRawAsync(
                new { symbol = "110010000002885", expiryDate = expiry });
        }

        // Try 4: bare RELIANCE with request string
        Console.WriteLine("       Trying RELIANCE + request...");
        result = await _client.GetOptionChainRawAsync(
            new { request = "optionchain", symbol = "RELIANCE", expiryDate = "2026-07-02" });

        // Try 5: NSEFO prefix with request string
        if (result == null)
        {
            Console.WriteLine("       Trying NSEFO: + request...");
            result = await _client.GetOptionChainRawAsync(
                new { request = "optionchain", symbol = "NSEFO:RELIANCE", expiryDate = "2026-07-02" });
        }

        // Try 6: NSECM prefix with request string
        if (result == null)
        {
            Console.WriteLine("       Trying NSECM: + request...");
            result = await _client.GetOptionChainRawAsync(
                new { request = "optionchain", symbol = "NSECM:RELIANCE", expiryDate = "2026-07-02" });
        }

        Console.WriteLine($"       Spot={result.Data?.SpotPrice} Expiry={result.Data?.ExpiryDate} ATM={result.Data?.Atm}");

        var atmStrike = result.Data?.Atm ?? 0;
        var chains = result.Data?.Chains ?? [];

        var atmEntry = chains.FirstOrDefault(c => Math.Abs(c.StrikePrice - atmStrike) < 0.01);
        if (atmEntry == null)
        {
            Console.WriteLine("       No ATM entry found in chain");
            return;
        }

        var callPremium = atmEntry.CallOption?.Price ?? atmEntry.CallOption?.Ltp ?? 0;
        var putPremium  = atmEntry.PutOption?.Price  ?? atmEntry.PutOption?.Ltp  ?? 0;
        var straddleCost = callPremium + putPremium;

        Console.WriteLine($"       Strike    : {atmEntry.StrikePrice}");
        Console.WriteLine($"       Call Prem : {callPremium}  (LTP={atmEntry.CallOption?.Ltp}, Delta={atmEntry.CallOption?.Delta}, IV={atmEntry.CallOption?.Iv})");
        Console.WriteLine($"       Put Prem  : {putPremium}  (LTP={atmEntry.PutOption?.Ltp}, Delta={atmEntry.PutOption?.Delta}, IV={atmEntry.PutOption?.Iv})");
        Console.WriteLine($"       ───────────────────────────────");
        Console.WriteLine($"       STRADDLE  : {straddleCost}  (Call + Put)");
    }

    static async Task TestGetMarketQuoteByIds()
    {
        var ids = _xml.Root!.Elements("Instrument").Select(e => long.Parse(e.Attribute("Id")!.Value)).ToList();
        var result = await _client.GetMarketQuoteAsync(ids);
        Console.WriteLine($"       status={result.Status} entries={result.Data?.Count}");
        if (result.Data != null)
            foreach (var (key, entry) in result.Data.Take(3))
                Console.WriteLine($"       {key}: LTP={entry.Ltp} OI={entry.Oi} Vol={entry.Vtt}");
    }

    static async Task TestGetMarketQuoteByNames()
    {
        var names = XmlList("MarketData", "Symbol").ToList();
        var result = await _client.GetMarketQuoteAsync(names);
        Console.WriteLine($"       status={result.Status} entries={result.Data?.Count}");
    }

    static async Task TestGetHistoricalData()
    {
        var symbol = Xml("MarketData/HistoricalSymbol");
        var interval = Xml("MarketData/HistoricalInterval");
        var result = await _client.GetHistoricalDataAsync(symbol, interval);
        Console.WriteLine($"       got {result.Count} daily candles");
        if (result.Count > 0)
        {
            var last = result[^1];
            Console.WriteLine($"       latest: O={last.Open} H={last.High} L={last.Low} C={last.Close} V={last.Volume} @ {last.Timestamp}");
        }
    }

    static async Task TestGetInstruments()
    {
        var result = await _client.GetInstrumentsAsync();
        Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
        if (result.Data != null)
            foreach (var inst in result.Data.Take(3))
                Console.WriteLine($"       id={inst.InstrumentId} symbol={inst.Symbol} ltp={inst.Ltp}");
    }

    static async Task TestGetOrderBook()
    {
        var id = XmlLongAttr("Instrument", "Id");
        var result = await _client.GetOrderBookAsync(id);
        Console.WriteLine($"       status={result.Status} instrumentId={result.Data?.InstrumentId}");
        if (result.Data != null)
        {
            Console.WriteLine($"       bid levels={result.Data.Bid.Count} ask levels={result.Data.Ask.Count}");
            foreach (var level in result.Data.Bid.Take(3))
                Console.WriteLine($"       bid  price={level.Price} qty={level.Quantity} orders={level.Orders}");
            foreach (var level in result.Data.Ask.Take(3))
                Console.WriteLine($"       ask  price={level.Price} qty={level.Quantity} orders={level.Orders}");
        }
    }

    static async Task TestGetOrders()
    {

        var result = await _client.GetOrdersAsync();
        Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
        foreach (var o in (result.Data ?? []).Take(3))
            Console.WriteLine($"       OrderID={o.BlitzOrderId}");
    }

    static async Task TestGetOpenOrders()
    {
        var result = await _client.GetOpenOrdersAsync();
        Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
    }

    static async Task TestGetPositions()
    {
        var result = await _client.GetPositionsAsync();
        Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
    }

    static async Task TestGetTrades()
    {
        var result = await _client.GetTradesAsync();
        Console.WriteLine($"       status={result.Status} count={result.Data?.Count}");
    }

    static async Task TestPlaceAndModifyOrderCycle()
    {
        var instrumentId = XmlLong("PlaceOrder/InstrumentId");
        var ltpResp = await _client.GetLtpAsync(new List<long> { instrumentId });
        var ltp = ltpResp.Data?.Values.FirstOrDefault()?.Ltp ?? 0;
        Console.WriteLine($"       LTP={ltp}");

        if (ltp <= 0)
        {
            Console.WriteLine("       could not fetch LTP, falling back to XML price");
            ltp = XmlDouble("PlaceOrder/Price");
        }

        var placePrice = Math.Round(ltp * 0.95, 2);
        var placeRequest = new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = XmlInt("PlaceOrder/Quantity"),
            Product = Xml("PlaceOrder/Product"),
            Tif = Xml("PlaceOrder/Tif"),
            Price = placePrice,
            OrderType = Xml("PlaceOrder/OrderType"),
            OrderSide = Xml("PlaceOrder/OrderSide"),
            DisclosedQuantity = XmlInt("PlaceOrder/DisclosedQuantity"),
            StopPrice = XmlDouble("PlaceOrder/StopPrice"),
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = instrumentId,
            ClientId = Xml("PlaceOrder/ClientId"),
        };

        var placeResult = await _client.PlaceOrderAsync(placeRequest);
        Console.WriteLine($"       place status={placeResult.Status} message={placeResult.Message}");

        if (placeResult.Data is null)
        {
            Console.WriteLine("       no data in place response, cannot proceed with modify");
            return;
        }

        var orderId = placeResult.Data.BlitzOrderId;
        Console.WriteLine($"       placed orderId={orderId} price={placePrice}");

        var modifyPrice = Math.Round(placePrice * 1.01, 2);
        Console.WriteLine($"       modify price 1% higher: {modifyPrice}, same qty {XmlInt("PlaceOrder/Quantity")}");

        var modifyResult = await _client.ModifyOrderAsync(new ModifyOrderRequest
        {
            BlitzOrderId = orderId,
            ModifiedOrderQuantity = XmlInt("PlaceOrder/Quantity"),
            Price = modifyPrice,
            OrderType = "LIMIT",
            Tif = "GFD",
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = XmlLong("PlaceOrder/InstrumentId"),
            Symbol = null,
        });
        Console.WriteLine($"       modify status={modifyResult.Status} message={modifyResult.Message}");
    }

    static async Task TestPlaceAndCancelOrderCycle()
    {
        var placeResult = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = XmlInt("PlaceOrder/Quantity"),
            Product = Xml("PlaceOrder/Product"),
            Tif = Xml("PlaceOrder/Tif"),
            Price = XmlDouble("PlaceOrder/Price"),
            OrderType = Xml("PlaceOrder/OrderType"),
            OrderSide = Xml("PlaceOrder/OrderSide"),
            DisclosedQuantity = XmlInt("PlaceOrder/DisclosedQuantity"),
            StopPrice = XmlDouble("PlaceOrder/StopPrice"),
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = XmlLong("PlaceOrder/InstrumentId"),
            ClientId = Xml("PlaceOrder/ClientId"),
        });
        Console.WriteLine($"       place status={placeResult.Status} message={placeResult.Message}");

        if (placeResult.Data is null)
        {
            Console.WriteLine("       no data in place response, cannot proceed with cancel");
            return;
        }

        var orderId = placeResult.Data.BlitzOrderId;
        Console.WriteLine($"       placed orderId={orderId}");

        var cancelResult = await _client.CancelOrderAsync(new CancelOrderRequest
        {
            BlitzOrderId = orderId,
            InstrumentId = XmlLong("PlaceOrder/InstrumentId"),
        });
        Console.WriteLine($"       cancel status={cancelResult.Status} message={cancelResult.Message}");
    }

    static async Task TestPlaceOrder()
    {
        var result = await _client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = XmlInt("PlaceOrder/Quantity"),
            Product = Xml("PlaceOrder/Product"),
            Tif = Xml("PlaceOrder/Tif"),
            Price = XmlDouble("PlaceOrder/Price"),
            OrderType = Xml("PlaceOrder/OrderType"),
            OrderSide = Xml("PlaceOrder/OrderSide"),
            DisclosedQuantity = XmlInt("PlaceOrder/DisclosedQuantity"),
            StopPrice = XmlDouble("PlaceOrder/StopPrice"),
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = XmlLong("PlaceOrder/InstrumentId"),
            ClientId = Xml("PlaceOrder/ClientId"),
        });
        Console.WriteLine($"       status={result.Status} message={result.Message}");
    }

    static async Task TestModifyOrder()
    {
        var result = await _client.ModifyOrderAsync(new ModifyOrderRequest
        {
            BlitzOrderId = XmlLong("ModifyOrder/BlitzOrderId"),
            ModifiedOrderQuantity = XmlInt("ModifyOrder/ModifiedOrderQuantity"),
            Price = XmlDouble("ModifyOrder/Price"),
            OrderType = Xml("ModifyOrder/OrderType"),
            Tif = Xml("ModifyOrder/Tif"),
            DisclosedQuantity = XmlInt("ModifyOrder/DisclosedQuantity"),
            StopPrice = XmlDouble("ModifyOrder/StopPrice"),
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = XmlLong("ModifyOrder/InstrumentId"),
            Symbol = Xml("ModifyOrder/Symbol"),
        });
        Console.WriteLine($"       status={result.Status} message={result.Message}");
    }

    static async Task TestCancelOrder()
    {
        var result = await _client.CancelOrderAsync(new CancelOrderRequest
        {
            BlitzOrderId = XmlLong("CancelOrder/BlitzOrderId"),
            InstrumentId = XmlLong("CancelOrder/InstrumentId"),
        });
        Console.WriteLine($"       status={result.Status} message={result.Message}");
    }

    static async Task TestSendSignals()
    {
        var baseTime = DateTime.ParseExact(
            Xml("Signal/BaseTime"),
            "dd-MM-yyyy HH:mm:ss",
            null);

        var instrumentName = Xml("Signal/InstrumentName");

        var signals = new List<SignalRequest>
        {
            new SignalRequest
            {
                SourceStrategy = Xml("Signal/SourceStrategy"),
                DestinationStrategy = Xml("Signal/DestinationStrategy"),
                SourceSID = Xml("Signal/SourceSID"),
                InstanceRunningMode = Xml("Signal/InstanceRunningMode"),
                GlobalAction = Xml("Signal/GlobalAction"),
                Instruments = new List<SignalInstrument>
                {
                    new SignalInstrument
                    {
                        ExchangeSegment = Xml("Signal/ExchangeSegment"),
                        InstrumentName = instrumentName,
                        Action = Xml("Signal/Action"),
                        Lot = Xml("Signal/Lot"),
                        TimeStamp = baseTime.ToString("dd-MM-yyyy HH:mm:ss"),
                        InfoText = Xml("Signal/InfoText"),
                    }
                }
            }
        };

        var result = await _client.SendSignalsAsync(signals);
        Console.WriteLine($"       status={result.Status} message={result.Message}");
    }
}
