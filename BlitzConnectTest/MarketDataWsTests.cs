using System;
using System.Linq;
using System.Threading.Tasks;
using BlitzConnect.MarketData;

static class MarketDataWsTests
{
    public static async Task<int> RunAsync()
    {
        TestContext.Log("── Market Data WebSocket ────────────────────────");
        TestContext.TestAsync("MarketDataWS", TestMarketDataWebSocket);
        TestContext.Summary();
        return TestContext.Fail;
    }

    static async Task TestMarketDataWebSocket()
    {
        var baseUrl = TestContext.Config.MarketDataWsUrl;
        if (string.IsNullOrEmpty(baseUrl)) throw new System.Exception("No MarketDataWsUrl in config");
        var url = $"{baseUrl}?key={TestContext.Client.Token}";
        using var mdWs = new MarketDataWebSocket(url);

        int connectCount = 0;
        string? errorMsg = null;

        mdWs.OnMessage += (msg) =>
        {
            var json = Google.Protobuf.JsonFormatter.Default.Format(msg);
            TestContext.Log($"New tick data received:{json}");
        };
        mdWs.OnConnected += () =>
        {
            connectCount++;
            TestContext.Log($"  [WS] Connected (connect_count={connectCount})");
        };
        mdWs.OnError += (err) =>
        {
            errorMsg = err;
            TestContext.Log($"  [WS] Error: {err}");
        };
        mdWs.OnDisconnected += (code, reason) => TestContext.Log($"  [WS] Disconnected: code={code} reason={reason}");

        TestContext.Log("  Connecting...");
        await mdWs.ConnectAsync();

        await Task.Delay(2000);

        var ids = TestContext.Cfg.MarketData.InstrumentIds.Count > 0
            ? TestContext.Cfg.MarketData.InstrumentIds
            : TestContext.Cfg.Instruments.Select(i => i.Id).Take(2).ToList();
        TestContext.Log("  Subscribing...");
        await mdWs.SubscribeAsync(ids);
        TestContext.Log($"  [PASS] Subscribed to {string.Join(", ", ids)}");

        TestContext.Log("  Listening...");
        TestContext.Log("  Streaming market data — press Ctrl+C to stop.");
        var shutdown = new TaskCompletionSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            TestContext.Log("\n  Shutting down...");
            shutdown.TrySetResult();
        };
        await shutdown.Task;

        TestContext.Log($"  [INFO] Connect count: {connectCount}");
        if (errorMsg != null)
            TestContext.Log($"  [INFO] Errors: {errorMsg}");

        await mdWs.DisconnectAsync();
        TestContext.Log("       disconnected");
        TestContext.Log("  [PASS] MarketDataWS");
    }
}
