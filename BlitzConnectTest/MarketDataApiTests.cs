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
        TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/ltp", new { InstrumentIds = ids }));
    }

    static async Task GetLtpByNames()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/ltp", new { InstrumentIds = ids }));
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
        try
        {
            TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/optionchain", body));
        }
        catch
        {
            foreach (var fallback in md.OptionChainFallbackSymbols)
            {
                var fbBody = new { symbol = fallback, expiryDate = md.OptionChainExpiry, exchangeSegment = md.OptionChainExchangeSegment, instrumentId = md.OptionChainInstrumentId };
                try
                {
                    TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/optionchain", fbBody));
                    return;
                }
                catch { }
            }
            throw;
        }
    }

    static async Task GetNiftyAtmStraddle()
    {
        var md = TestContext.Cfg.MarketData;
        var body = new { symbol = "NIFTY", expiryDate = md.RelianceOptionChainExpiry, exchangeSegment = md.OptionChainExchangeSegment, instrumentId = md.OptionChainInstrumentId };
        TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/optionchain", body));
    }

    static async Task GetMarketQuoteByIds()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).ToList();
        TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/quote", new { InstrumentIds = ids }));
    }

    static async Task GetMarketQuoteByNames()
    {
        var ids = TestContext.Cfg.Instruments.Select(i => i.Id).ToList();
        TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/quote", new { InstrumentIds = ids }));
    }

    static async Task GetHistoricalData()
    {
        var md = TestContext.Cfg.MarketData;
        TestContext.Raw(await TestContext.Client.MarketDataRawAsync("marketfeed/historicalData",
            new { Instrument = md.HistoricalSymbol, interval = md.HistoricalInterval }, accept: "*/*"));
    }
}
