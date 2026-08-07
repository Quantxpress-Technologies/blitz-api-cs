using System.Text.Json;
using BlitzConnect.Common.Models;

namespace BlitzConnect.Common;

public interface IBlitzApiClient : IDisposable
{
    Task LoginAsync(CancellationToken ct = default);

    /// <summary>Gets instrument details by numeric ID.</summary>
    Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(long id, CancellationToken ct = default);

    /// <summary>Gets instrument details by symbol string.</summary>
    Task<BlitzApiResponse<InstrumentDetail>> GetInstrumentDetailsAsync(string symbol, CancellationToken ct = default);

    /// <summary>Gets all instruments (bulk).</summary>
    Task<BlitzApiResponse<List<InstrumentDetail>>> GetInstrumentsAsync(CancellationToken ct = default);

    /// <summary>Gets LTP for one or more instrument IDs.</summary>
    Task<LtpResponse> GetLtpAsync(List<long> ids, CancellationToken ct = default);

    /// <summary>Gets the option chain for a symbol/expiry.</summary>
    Task<OptionChainResponse> GetOptionChainAsync(string symbol, string expiryDate, CancellationToken ct = default);

    /// <summary>Sends a raw option-chain request body.</summary>
    Task<OptionChainResponse?> GetOptionChainRawAsync(object body, CancellationToken ct = default);

    /// <summary>Gets full market quote for instrument IDs.</summary>
    Task<MarketQuoteResponse> GetMarketQuoteAsync(List<long> ids, CancellationToken ct = default);

    /// <summary>Gets historical candle data.</summary>
    Task<List<HistoricalDataItem>> GetHistoricalDataAsync(string instrument, string interval, CancellationToken ct = default);

    /// <summary>Gets all orders (history).</summary>
    Task<OrdersResponse> GetOrdersAsync(CancellationToken ct = default);

    /// <summary>Gets open orders.</summary>
    Task<OrdersResponse> GetOpenOrdersAsync(CancellationToken ct = default);

    /// <summary>Gets current positions.</summary>
    Task<PositionsResponse> GetPositionsAsync(CancellationToken ct = default);

    /// <summary>Gets trade history.</summary>
    Task<TradesResponse> GetTradesAsync(CancellationToken ct = default);

    /// <summary>Gets a single order by BlitzOrderId.</summary>
    Task<BlitzApiResponse<OrderEntry>> GetOrderByIdAsync(long blitzOrderId, CancellationToken ct = default);

    /// <summary>Gets strategy statistics for the current user.</summary>
    Task<StrategyStatisticsResponse> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>Gets client strategy-instance-level statistics grouped by client.</summary>
    Task<StrategyInstanceStatisticsResponse> GetStatisticsByInstanceAsync(
        string strategyName, string strategyInstanceName, CancellationToken ct = default);

    /// <summary>Places a new order.</summary>
    Task<BlitzApiResponse<PlaceOrderData>> PlaceOrderAsync(PlaceOrderRequest order, CancellationToken ct = default);

    /// <summary>Modifies an existing order.</summary>
    Task<ModifyOrderResponse> ModifyOrderAsync(ModifyOrderRequest order, CancellationToken ct = default);

    /// <summary>Cancels an order.</summary>
    Task<GatewayResponse> CancelOrderAsync(CancelOrderRequest cancel, CancellationToken ct = default);

    /// <summary>Sends trading signals.</summary>
    Task<GatewayResponse> SendSignalsAsync(List<SignalRequest> signals, CancellationToken ct = default);
}
