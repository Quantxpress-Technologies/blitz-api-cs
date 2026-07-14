using BlitzConnect.Models;

namespace BlitzConnect.Services;

public interface IBlitzApiClient : IDisposable
{
    Task LoginAsync(CancellationToken ct = default);

    // Market Data
    Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id, CancellationToken ct = default);
    Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol, CancellationToken ct = default);
    Task<BlitzApiResponse<List<InstrumentDetail>>> GetInstrumentsAsync(CancellationToken ct = default);
    Task<LtpResponse> GetLtpAsync(List<long> ids, CancellationToken ct = default);
    Task<LtpResponse> GetLtpAsync(List<string> names, CancellationToken ct = default);
    Task<OptionChainResponse> GetOptionChainAsync(string symbol, string expiryDate, CancellationToken ct = default);
    Task<OptionChainResponse?> GetOptionChainRawAsync(object body, CancellationToken ct = default);
    Task<MarketQuoteResponse> GetMarketQuoteAsync(List<long> ids, CancellationToken ct = default);
    Task<MarketQuoteResponse> GetMarketQuoteAsync(List<string> names, CancellationToken ct = default);
    Task<List<HistoricalDataItem>> GetHistoricalDataAsync(string instrument, string interval, CancellationToken ct = default);
    Task<DepthResponse> GetOrderBookAsync(long instrumentId, CancellationToken ct = default);

    // Trading (read-only)
    Task<OrdersResponse> GetOrdersAsync(CancellationToken ct = default);
    Task<OrdersResponse> GetOpenOrdersAsync(CancellationToken ct = default);
    Task<PositionsResponse> GetPositionsAsync(CancellationToken ct = default);
    Task<TradesResponse> GetTradesAsync(CancellationToken ct = default);
    Task<BlitzApiResponse<OrderEntry>> GetOrderByIdAsync(long blitzOrderId, CancellationToken ct = default);
    Task<BlitzApiResponse<object>> GetTradeByIdAsync(long tradeId, CancellationToken ct = default);

    // Write operations
    Task<BlitzApiResponse<PlaceOrderData>> PlaceOrderAsync(PlaceOrderRequest order, CancellationToken ct = default);
    Task<BlitzApiResponse<PlaceOrderData>> ModifyOrderAsync(ModifyOrderRequest order, CancellationToken ct = default);
    Task<BlitzApiResponse<object>> CancelOrderAsync(CancelOrderRequest cancel, CancellationToken ct = default);
    Task<BlitzApiResponse<object>> SendSignalsAsync(List<SignalRequest> signals, CancellationToken ct = default);
}
