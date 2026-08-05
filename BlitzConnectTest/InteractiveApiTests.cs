using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlitzConnect.Common.Models;

static class InteractiveApiTests
{
    public static async Task<int> RunAsync()
    {
        TestContext.Log("── Interactive API ───────────────────────────");
        TestContext.TestAsync("GetOrders", GetOrders);
        TestContext.TestAsync("GetOpenOrders", GetOpenOrders);
        TestContext.TestAsync("GetPositions", GetPositions);
        TestContext.TestAsync("GetTrades", GetTrades);
        TestContext.TestAsync("GetOrderById", GetOrderById);
        TestContext.TestAsync("GetTradeById", GetTradeById);
        TestContext.TestAsync("PlaceAndCancelCycle", PlaceAndCancelOrderCycle);
        TestContext.TestAsync("PlaceAndModifyCycle", PlaceAndModifyOrderCycle);
        TestContext.TestAsync("SendSignals", SendSignals);
        TestContext.Summary();
        return TestContext.Fail;
    }

    static async Task GetOrders()
    {
        var result = await TestContext.Client.GetOrdersAsync();
        TestContext.Log($"       count={result.Count}");
        foreach (var o in result.Data.Take(3))
            TestContext.Log($"       OrderID={o.BlitzOrderId}");
    }

    static async Task GetOpenOrders()
    {
        var result = await TestContext.Client.GetOpenOrdersAsync();
        TestContext.Log($"       count={result.Count}");
    }

    static async Task GetPositions()
    {
        var result = await TestContext.Client.GetPositionsAsync();
        TestContext.Log($"       count={result.Count}");
    }

    static async Task GetTrades()
    {
        var result = await TestContext.Client.GetTradesAsync();
        TestContext.Log($"       count={result.Count}");
    }

    static async Task GetOrderById()
    {
        var orders = await TestContext.Client.GetOrdersAsync();
        var id = orders.Data.FirstOrDefault()?.BlitzOrderId ?? TestContext.Cfg.CancelOrder.BlitzOrderId;
        var result = await TestContext.Client.GetOrderByIdAsync(id);
        TestContext.Log($"       orderId={id} status={result.Status} found={result.Data != null}");
    }

    static async Task GetTradeById()
    {
        var trades = await TestContext.Client.GetTradesAsync();
        if (trades.Count == 0) { TestContext.Log("       no trades to test"); return; }
        var el = trades.Data[0];
        long tradeId = 0;
        if (el.TryGetProperty("tradeId", out var tid)) tradeId = tid.GetInt64();
        else if (el.TryGetProperty("TradeId", out var tid2)) tradeId = tid2.GetInt64();
        else if (el.TryGetProperty("blitzTradeId", out var tid3)) tradeId = tid3.GetInt64();
        else if (el.TryGetProperty("id", out var tid4)) tradeId = tid4.GetInt64();
        if (tradeId == 0) { TestContext.Log("       no trade id found in response"); return; }
        var result = await TestContext.Client.GetTradeByIdAsync(tradeId);
        TestContext.Log($"       tradeId={tradeId} status={result.Status}");
    }

    static async Task PlaceAndModifyOrderCycle()
    {
        var po = TestContext.Cfg.PlaceOrder;
        var ltpResp = await TestContext.Client.GetLtpAsync(new List<long> { po.InstrumentId });
        var ltp = ltpResp.Data?.Values.FirstOrDefault()?.Ltp ?? po.Price;
        var placePrice = Math.Round(ltp * 0.95, 2);

        var placeResult = await TestContext.Client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = placePrice, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = po.ClientId,
        });
        TestContext.Log($"       place status={placeResult.Status} message={placeResult.Message}");
        if (placeResult.Data is null) { TestContext.Log("       no data, skip modify"); return; }

        var orderId = placeResult.Data.BlitzOrderId;
        TestContext.Log($"       placed orderId={orderId} price={placePrice}");

        var modifyPrice = Math.Round(placePrice * 1.01, 2);
        var modifyResult = await TestContext.Client.ModifyOrderAsync(new ModifyOrderRequest
        {
            BlitzOrderId = orderId, ModifiedOrderQuantity = po.Quantity, Price = modifyPrice,
            OrderType = po.OrderType, Tif = po.Tif, TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, Symbol = null,
        });
        TestContext.Log($"       modify status={modifyResult.Status} message={modifyResult.Message}");
    }

    static async Task PlaceAndCancelOrderCycle()
    {
        var po = TestContext.Cfg.PlaceOrder;
        var placeResult = await TestContext.Client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = po.Price, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = po.ClientId,
        });
        TestContext.Log($"       place status={placeResult.Status} message={placeResult.Message}");
        if (placeResult.Data is null) { TestContext.Log("       no data, skip cancel"); return; }

        var cancelResult = await TestContext.Client.CancelOrderAsync(new CancelOrderRequest
        {
            BlitzOrderId = placeResult.Data.BlitzOrderId,
            InstrumentId = po.InstrumentId,
        });
        TestContext.Log($"       cancel status={cancelResult.Status} message={cancelResult.Message}");
    }

    static async Task SendSignals()
    {
        var sg = TestContext.Cfg.Signal;
        var baseTime = DateTime.ParseExact(sg.BaseTime, "dd-MM-yyyy HH:mm:ss", null);
        var result = await TestContext.Client.SendSignalsAsync(new List<SignalRequest>
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
        TestContext.Log($"       status={result.Status} message={result.Message}");
    }
}
