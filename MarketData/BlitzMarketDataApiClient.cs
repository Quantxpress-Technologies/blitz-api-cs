using BlitzConnect.Common;
using BlitzConnect.Common.Models;

namespace BlitzConnect.MarketData;

public class BlitzMarketDataApiClient
{
    private readonly BlitzApiClient _inner;

    public BlitzMarketDataApiClient(BlitzConfig config)
    {
        _inner = new BlitzApiClient(config);
    }

    public Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id, CancellationToken ct = default) =>
        _inner.GetInstrumentDetailsAsync(id, ct);
    public Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol, CancellationToken ct = default) =>
        _inner.GetInstrumentDetailsAsync(symbol, ct);
    public Task<BlitzApiResponse<List<InstrumentDetail>>> GetInstrumentsAsync(CancellationToken ct = default) =>
        _inner.GetInstrumentsAsync(ct);
    public Task<LtpResponse> GetLtpAsync(List<long> ids, CancellationToken ct = default) =>
        _inner.GetLtpAsync(ids, ct);
    public Task<OptionChainResponse> GetOptionChainAsync(string symbol, string expiryDate, CancellationToken ct = default) =>
        _inner.GetOptionChainAsync(symbol, expiryDate, ct);
    public Task<OptionChainResponse?> GetOptionChainRawAsync(object body, CancellationToken ct = default) =>
        _inner.GetOptionChainRawAsync(body, ct);
    public Task<MarketQuoteResponse> GetMarketQuoteAsync(List<long> ids, CancellationToken ct = default) =>
        _inner.GetMarketQuoteAsync(ids, ct);
    public Task<List<HistoricalDataItem>> GetHistoricalDataAsync(string instrument, string interval, CancellationToken ct = default) =>
        _inner.GetHistoricalDataAsync(instrument, interval, ct);
}
