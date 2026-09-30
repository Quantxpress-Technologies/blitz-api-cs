using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlitzConnect.Common.Models;

static class InteractiveApiTests
{
    public static Task<int> RunAsync()
    {
        TestContext.Log("── Interactive API ───────────────────────────");
        TestContext.TestAsync("GetProfile", GetProfile);
        TestContext.TestAsync("GetHoldings", GetHoldings);
        TestContext.TestAsync("GetOrders", GetOrders);
        TestContext.TestAsync("GetOpenOrders", GetOpenOrders);
        TestContext.TestAsync("GetPositions", GetPositions);
        TestContext.TestAsync("GetTrades", GetTrades);
        TestContext.TestAsync("GetTradesByBlitzOrderId", GetTradesByBlitzOrderId);
        TestContext.TestAsync("GetOrderById", GetOrderById);
        TestContext.TestAsync("GetStatistics", GetStatistics);
        TestContext.TestAsync("GetStatisticsByInstance", GetStatisticsByInstance);
        TestContext.TestAsync("PlaceOrder", PlaceOrder);
        TestContext.TestAsync("ModifyOrder", ModifyOrder);
        TestContext.TestAsync("CancelOrder", CancelOrder);
        TestContext.TestAsync("Logout", Logout);
        // SendSignals posts to a live strategy and can trigger real trades, so it stays opt-in.
        //TestContext.TestAsync("SendSignals", SendSignals);
        return Task.FromResult(TestContext.Fail);
    }

    static async Task GetProfile()
    {
        var profile = await TestContext.Client.GetProfileAsync();
        TestContext.Raw(JsonSerializer.Serialize(profile));
    }

    static async Task GetHoldings()
    {
        var holdings = await TestContext.Client.GetHoldingsAsync();
        TestContext.Raw(JsonSerializer.Serialize(holdings.Data));
    }

    static async Task Logout()
    {
        var resp = await TestContext.Client.LogoutAsync();
        TestContext.Raw(JsonSerializer.Serialize(resp));
        if (resp.Message is null)
            throw new Exception($"Unexpected logout response: {JsonSerializer.Serialize(resp)}");
    }

    static async Task GetOrders() =>
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, "orders"));

    static async Task GetOpenOrders() =>
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, "orders/openOrders"));

    static async Task GetPositions() =>
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, "positions"));

    static async Task GetTrades() =>
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, "trades"));

    static async Task GetTradesByBlitzOrderId()
    {
        long id = 222115050150000049; // temp: fixed id requested by user
        try
        {
            TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, $"trades/{id}"));
        }
        catch (BlitzConnect.Common.BlitzConnectException ex) when (ex.HttpStatusCode == 404)
        {
            TestContext.Log($"       404 - no trades for BlitzOrderId {id}");
        }
    }

    static async Task GetOrderById()
    {
        var ordersRaw = await TestContext.Client.TradingRawAsync(HttpMethod.Get, "orders");
        var orders = System.Text.Json.JsonSerializer.Deserialize<List<OrderEntry>>(ordersRaw,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        var id = orders.FirstOrDefault()?.BlitzOrderId ?? TestContext.Cfg.CancelOrder.BlitzOrderId;
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, $"orders/{id}"));
    }

    static async Task GetStatistics() =>
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, "strategy/statistics"));

    static async Task GetStatisticsByInstance()
    {
        var query = new List<string>
        {
            $"strategyName={Uri.EscapeDataString(TestContext.Cfg.Statistics.StrategyName)}",
            $"strategyInstanceName={Uri.EscapeDataString(TestContext.Cfg.Statistics.StrategyInstanceName)}"
        };
        TestContext.Raw(await TestContext.Client.TradingRawAsync(HttpMethod.Get, $"strategy/statistics/instance?{string.Join("&", query)}"));
    }

    static long? ResolveBlitzOrderId(long configured)
    {
        if (configured != 0) return configured;
        if (TestContext.LastPlacedBlitzOrderId is not null) return TestContext.LastPlacedBlitzOrderId;

        var ordersRaw = TestContext.Client.TradingRawAsync(HttpMethod.Get, "orders").GetAwaiter().GetResult();
        var orders = System.Text.Json.JsonSerializer.Deserialize<List<OrderEntry>>(ordersRaw,
            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        return orders.FirstOrDefault()?.BlitzOrderId;
    }

    static async Task PlaceOrder()
    {
        var po = TestContext.Cfg.PlaceOrder;
        var ltpResp = await TestContext.Client.GetLtpAsync(new List<long> { po.InstrumentId });
        var ltp = ltpResp.Data?.Values.FirstOrDefault()?.Ltp ?? po.Price;
        var placePrice = Math.Round(ltp * 0.95, 2);

        var result = await TestContext.Client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = placePrice, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = TestContext.Cfg.Connection.ClientId ?? "",
            ExchangeSegment = po.ExchangeSegment,
        });
        TestContext.Log($"       status={result.Status} message={result.Message}");
        if (result.Data is null)
            throw new Exception($"place order returned no data: {result.Message}");
        TestContext.LastPlacedBlitzOrderId = result.Data.BlitzOrderId;
        TestContext.Log($"       blitzOrderId={result.Data.BlitzOrderId} price={placePrice}");
    }

    static async Task ModifyOrder()
    {
        var mo = TestContext.Cfg.ModifyOrder;
        var id = ResolveBlitzOrderId(mo.BlitzOrderId);
        if (id is null or 0)
            throw new Exception("no blitzOrderId available - set modifyOrder.blitzOrderId in test-config.json");

        var price = mo.Price;
        if (price <= 0)
        {
            var ltpResp = await TestContext.Client.GetLtpAsync(new List<long> { mo.InstrumentId });
            var ltp = ltpResp.Data?.Values.FirstOrDefault()?.Ltp ?? mo.Price;
            price = Math.Round(ltp * 0.95, 2);
        }

        var result = await TestContext.Client.ModifyOrderAsync(new ModifyOrderRequest
        {
            BlitzOrderId = id.Value,
            ModifiedOrderQuantity = mo.ModifiedOrderQuantity,
            Price = price, OrderType = mo.OrderType, Tif = mo.Tif,
            DisclosedQuantity = mo.DisclosedQuantity, StopPrice = mo.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = mo.InstrumentId, Symbol = mo.Symbol,
            ExchangeSegment = mo.ExchangeSegment,
        });
        TestContext.Log($"       blitzOrderId={id} status={result.Status} message={result.Message} data={result.Data}");    }

    static async Task CancelOrder()
    {
        var co = TestContext.Cfg.CancelOrder;
        var id = ResolveBlitzOrderId(co.BlitzOrderId);
        if (id is null or 0)
            throw new Exception("no blitzOrderId available - set cancelOrder.blitzOrderId in test-config.json");

        var result = await TestContext.Client.CancelOrderAsync(new CancelOrderRequest
        {
            BlitzOrderId = id.Value,
            InstrumentId = co.InstrumentId,
            Symbol = co.Symbol,
            ExchangeSegment = TestContext.Cfg.PlaceOrder.ExchangeSegment,
        });
        TestContext.Log($"       blitzOrderId={id} status={result.Status} message={result.Message}");
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
