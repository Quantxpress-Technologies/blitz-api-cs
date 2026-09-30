using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlitzConnect.Interactive;
using BlitzConnect.MarketData;

// Read-only WebSocket smoke test. Mirrors WebSocket-Test-Script.py from the
// Python SDK so both SDKs can be validated the same way. Places no orders.
static class WsSmokeTests
{
    public static async Task<int> RunAsync(int seconds)
    {
        TestContext.Log("── WebSocket Smoke Test (read-only) ────────");
        await InteractiveSmokeAsync(seconds);
        await MarketDataSmokeAsync(seconds);
        TestContext.Summary();
        return TestContext.Fail;
    }

    static async Task InteractiveSmokeAsync(int seconds)
    {
        await TestContext.RunAsync("InteractiveWS", async () =>
        {
            var ws = new BlitzWebSocketClient(TestContext.Config);
            var connected = new TaskCompletionSource();
            var counts = new SortedDictionary<int, int>();
            var samples = new List<string>();
            var errors = new List<string>();

            ws.OnConnect += () => connected.TrySetResult();
            ws.OnMessage += m =>
            {
                var code = m.MessageCode ?? 0;
                counts[code] = counts.GetValueOrDefault(code) + 1;
                if (samples.Count < 3 && m.Body is not null)
                    samples.Add($"code={code} body={Truncate(m.Body.Value.GetRawText(), 160)}");
            };
            ws.OnError += e => errors.Add(e.Message);
            ws.OnClose += (c, r) => TestContext.Log($"  [IW] closed code={c} reason={r}");

            ws.Start(TestContext.Client.Token);

            await connected.Task.WaitAsync(TimeSpan.FromSeconds(15));
            TestContext.Log($"  [IW] connected to {TestContext.Config.InteractiveWsUrl}");

            await ws.SubscribeActionAsync("AllSubscribe");
            TestContext.Log("  [IW] sent AllSubscribe");

            await Task.Delay(TimeSpan.FromSeconds(seconds));

            TestContext.Log($"  [IW] messages: {counts.Values.Sum()} (heartbeat every {TestContext.Config.HeartbeatIntervalSeconds}s)");
            foreach (var kv in counts)
                TestContext.Log($"  [IW]   messageCode {kv.Key}: {kv.Value}");
            foreach (var s in samples)
                TestContext.Log($"  [IW]   sample {s}");
            foreach (var e in errors)
                TestContext.Log($"  [IW]   error {e}");

            await ws.StopAsync();
            ws.Dispose();

            if (counts.Count == 0)
                throw new Exception($"no messages received in {seconds}s");
        });
    }

    static async Task MarketDataSmokeAsync(int seconds)
    {
        await TestContext.RunAsync("MarketDataWS", async () =>
        {
            var baseUrl = TestContext.Config.MarketDataWsUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new Exception("No MarketDataWsUrl in config");

            using var md = new MarketDataWebSocket(
                baseUrl, TestContext.Client.Token, TestContext.Config.SkipCertificateValidation);

            var connected = new TaskCompletionSource();
            var subtypes = new SortedDictionary<string, int>();
            var samples = new List<string>();
            var errors = new List<string>();
            int total = 0;

            md.OnConnected += () => connected.TrySetResult();
            md.OnError += e => errors.Add(e);
            md.OnDisconnected += (c, r) => TestContext.Log($"  [MW] closed code={c} reason={r}");
            md.OnMessage += msg =>
            {
                Interlocked.Increment(ref total);
                var name = msg.SubtypeCase.ToString();
                subtypes[name] = subtypes.GetValueOrDefault(name) + 1;
            };
            md.OnMarketDepth += depth =>
            {
                if (samples.Count < 3)
                    samples.Add(
                        $"instrumentId={depth.InstrumentID} ltp={depth.LTP} " +
                        $"bidQty={depth.TBQ} askQty={depth.TSQ}");
            };

            await md.ConnectAsync();
            await connected.Task.WaitAsync(TimeSpan.FromSeconds(15));
            TestContext.Log($"  [MW] connected to {baseUrl}");

            var ids = TestContext.Cfg.MarketData.InstrumentIds.Count > 0
                ? TestContext.Cfg.MarketData.InstrumentIds
                : TestContext.Cfg.Instruments.Select(i => i.Id).Take(2).ToList();

            await Task.Delay(1000);
            await md.SubscribeAsync(ids);
            TestContext.Log($"  [MW] subscribed to {string.Join(", ", ids)}");

            await Task.Delay(TimeSpan.FromSeconds(seconds));

            TestContext.Log($"  [MW] messages: {total}");
            foreach (var kv in subtypes)
                TestContext.Log($"  [MW]   {kv.Key}: {kv.Value}");
            foreach (var s in samples)
                TestContext.Log($"  [MW]   sample depth {s}");
            foreach (var e in errors)
                TestContext.Log($"  [MW]   error {e}");

            await md.DisconnectAsync();

            // Ticks are market-dependent, so a quiet window is not a failure;
            // only a failed connect or subscribe above is.
            if (total == 0)
                TestContext.Log($"  [WARN] connected and subscribed, but no ticks in {seconds}s");
        });
    }

    static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
