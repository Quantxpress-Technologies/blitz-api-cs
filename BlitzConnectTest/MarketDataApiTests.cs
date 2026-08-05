using System;
using System.Linq;
using System.Threading.Tasks;

static class MarketDataApiTests
{
    public static async Task<int> RunAsync()
    {
        TestContext.Log("── Instrument Details ──────────────────────────");
        TestContext.TestAsync("GetInstrumentDetails.ById", GetInstrumentDetailsById);
        TestContext.TestAsync("GetInstrumentDetails.BySymbol", GetInstrumentDetailsBySymbol);
        TestContext.TestAsync("GetInstruments", GetInstruments);

        TestContext.Log(string.Empty);
        TestContext.Log("── Market Data ──────────────────────────────────");
        TestContext.TestAsync("GetLTP.ByIds", GetLtpByIds);
        TestContext.TestAsync("GetLTP.ByNames", GetLtpByNames);
        TestContext.TestAsync("GetOptionChain", GetOptionChain);
        TestContext.TestAsync("GetNiftyAtmStraddle", GetNiftyAtmStraddle);
        TestContext.TestAsync("GetMarketQuote.ByIds", GetMarketQuoteByIds);
        TestContext.TestAsync("GetMarketQuote.ByNames", GetMarketQuoteByNames);
        TestContext.TestAsync("GetHistoricalData", GetHistoricalData);
        TestContext.Summary();
        return TestContext.Fail;
    }

    static async Task GetInstrumentDetailsById()
    {
        var id = TestContext.Cfg.Instruments.Select(i => i.Id).FirstOrDefault();
        var result = await TestContext.Client.GetInstrumentDetailsAsync(id);
        TestContext.Log($"       status={result.Status} id={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
    }

    static async Task GetInstrumentDetailsBySymbol()
    {
        var symbol = TestContext.Cfg.Instruments.Select(i => i.Symbol).FirstOrDefault() ?? "";
        var result = await TestContext.Client.GetInstrumentDetailsAsync(symbol);
        TestContext.Log($"       status={result.Status} id={result.Data?.InstrumentId} symbol={result.Data?.Symbol} ltp={result.Data?.Ltp}");
    }

    static async Task GetInstruments()
    {
        var result = await TestContext.Client.GetInstrumentsAsync();
        TestContext.Log($"       status={result.Status} count={result.Data?.Count}");
        if (result.Data?.Count > 0)
        {
            var first = result.Data[0];
            TestContext.Log($"       first: id={first.InstrumentId} symbol={first.Symbol}");
        }
    }

    static async Task GetLtpByIds()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        var result = await TestContext.Client.GetLtpAsync(ids);
        TestContext.Log($"       status={result.Status} keys: {string.Join(", ", result.Data?.Keys ?? Enumerable.Empty<string>())}");
        if (result.Data != null)
            foreach (var (k, v) in result.Data)
                TestContext.Log($"       {k}: LTP={v.Ltp}");
    }

    static async Task GetLtpByNames()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        var result = await TestContext.Client.GetLtpAsync(ids);
        TestContext.Log($"       status={result.Status} keys: {string.Join(", ", result.Data?.Keys ?? Enumerable.Empty<string>())}");
        if (result.Data != null)
            foreach (var (k, v) in result.Data)
                TestContext.Log($"       {k}: LTP={v.Ltp}");
    }

    static async Task GetOptionChain()
    {
        var md = TestContext.Cfg.MarketData;
        var body = new
        {
            symbol = md.OptionChainSymbol,
            expiryDate = md.OptionChainExpiry,
            exchangeSegment = md.OptionChainExchangeSegment,
            instrumentId = md.OptionChainInstrumentId,
        };
        var result = await TestContext.Client.GetOptionChainRawAsync(body);
        if (result == null)
        {
            foreach (var fallback in md.OptionChainFallbackSymbols)
            {
                var fbBody = new { symbol = fallback, expiryDate = md.OptionChainExpiry, exchangeSegment = md.OptionChainExchangeSegment, instrumentId = md.OptionChainInstrumentId };
                result = await TestContext.Client.GetOptionChainRawAsync(fbBody);
                if (result != null) break;
            }
        }
        TestContext.Log($"       spot={result?.Data?.SpotPrice} expiry={result?.Data?.ExpiryDate} chains={result?.Data?.Chains.Count}");
        if (result?.Data?.Chains.Count > 0)
        {
            var first = result.Data.Chains[0];
            TestContext.Log($"       strike={first.StrikePrice} callLTP={first.CallOption?.Ltp} putLTP={first.PutOption?.Ltp}");
        }
    }

    static async Task GetNiftyAtmStraddle()
    {
        var md = TestContext.Cfg.MarketData;
        var body = new { symbol = "NIFTY", expiryDate = md.RelianceOptionChainExpiry, exchangeSegment = md.OptionChainExchangeSegment, instrumentId = md.OptionChainInstrumentId };
        var result = await TestContext.Client.GetOptionChainRawAsync(body);
        TestContext.Log($"       Spot={result?.Data?.SpotPrice} Expiry={result?.Data?.ExpiryDate} ATM={result?.Data?.Atm}");
        var chains = result?.Data?.Chains ?? [];
        var atmEntry = chains.FirstOrDefault(c => Math.Abs(c.StrikePrice - (result?.Data?.Atm ?? 0)) < 0.01);
        if (atmEntry == null) { TestContext.Log("       No ATM entry found"); return; }
        var callP = atmEntry.CallOption?.Price ?? atmEntry.CallOption?.Ltp ?? 0;
        var putP  = atmEntry.PutOption?.Price  ?? atmEntry.PutOption?.Ltp  ?? 0;
        TestContext.Log($"       Strike={atmEntry.StrikePrice} Call={callP} Put={putP} Straddle={callP + putP}");
    }

    static async Task GetMarketQuoteByIds()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).ToList();
        var result = await TestContext.Client.GetMarketQuoteAsync(ids);
        TestContext.Log($"       status={result.Status} entries={result.Data?.Count}");
        if (result.Data != null)
            foreach (var (k, v) in result.Data.Take(3))
                TestContext.Log($"       {k}: LTP={v.Ltp} OI={v.Oi} Vol={v.Vtt}");
    }

    static async Task GetMarketQuoteByNames()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).ToList();
        var result = await TestContext.Client.GetMarketQuoteAsync(ids);
        TestContext.Log($"       status={result.Status} entries={result.Data?.Count}");
    }

    static async Task GetHistoricalData()
    {
        var md = TestContext.Cfg.MarketData;
        var result = await TestContext.Client.GetHistoricalDataAsync(md.HistoricalSymbol, md.HistoricalInterval);
        TestContext.Log($"       got {result.Count} candles");
        if (result.Count > 0)
        {
            var last = result[^1];
            TestContext.Log($"       latest: O={last.Open} H={last.High} L={last.Low} C={last.Close} V={last.Volume} @ {last.Timestamp}");
        }
    }
}
