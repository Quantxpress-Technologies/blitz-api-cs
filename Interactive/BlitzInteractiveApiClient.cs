using System.Text.Json;
using BlitzConnect.Common;
using BlitzConnect.Common.Models;

namespace BlitzConnect.Interactive;

public class BlitzInteractiveApiClient
{
    private readonly BlitzApiClient _inner;

    public BlitzInteractiveApiClient(BlitzConfig config)
    {
        Config = config;
        _inner = new BlitzApiClient(config);
    }

    public BlitzConfig Config { get; }
    public string? Token { get; private set; }

    public async Task LoginAsync(CancellationToken ct = default)
    {
        await _inner.LoginAsync(ct);
        Token = _inner.Token;
    }

    public Task<OrdersResponse> GetOrdersAsync(CancellationToken ct = default) =>
        _inner.GetOrdersAsync(ct);
    public Task<OrdersResponse> GetOpenOrdersAsync(CancellationToken ct = default) =>
        _inner.GetOpenOrdersAsync(ct);
    public Task<PositionsResponse> GetPositionsAsync(CancellationToken ct = default) =>
        _inner.GetPositionsAsync(ct);
    public Task<TradesResponse> GetTradesAsync(CancellationToken ct = default) =>
        _inner.GetTradesAsync(ct);
    public Task<BlitzApiResponse<OrderEntry>> GetOrderByIdAsync(long blitzOrderId, CancellationToken ct = default) =>
        _inner.GetOrderByIdAsync(blitzOrderId, ct);
    public Task<StrategyStatisticsResponse> GetStatisticsAsync(CancellationToken ct = default) =>
        _inner.GetStatisticsAsync(ct);
    public Task<StrategyInstanceStatisticsResponse> GetStatisticsByInstanceAsync(
        string strategyName, string strategyInstanceName, CancellationToken ct = default) =>
        _inner.GetStatisticsByInstanceAsync(strategyName, strategyInstanceName, ct);
    public Task<BlitzApiResponse<PlaceOrderData>> PlaceOrderAsync(PlaceOrderRequest order, CancellationToken ct = default) =>
        _inner.PlaceOrderAsync(order, ct);
    public Task<ModifyOrderResponse> ModifyOrderAsync(ModifyOrderRequest order, CancellationToken ct = default) =>
        _inner.ModifyOrderAsync(order, ct);
    public Task<GatewayResponse> CancelOrderAsync(CancelOrderRequest cancel, CancellationToken ct = default) =>
        _inner.CancelOrderAsync(cancel, ct);
    public Task<GatewayResponse> SendSignalsAsync(List<SignalRequest> signals, CancellationToken ct = default) =>
        _inner.SendSignalsAsync(signals, ct);
}
