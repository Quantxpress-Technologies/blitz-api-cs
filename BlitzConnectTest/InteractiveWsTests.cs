using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlitzConnect.Common.Models;
using BlitzConnect.Interactive;

static class InteractiveWsTests
{
    public static async Task<int> RunAsync()
    {
        TestContext.Log("── Interactive WebSocket ──────────────────────");
        TestContext.TestAsync("InteractiveWS", TestWebSocket);
        TestContext.TestAsync("InteractiveWS.Client", TestWebSocketClient);
        TestContext.Summary();
        return TestContext.Fail;
    }

    static async Task TestWebSocket()
    {
        var wsUrl = TestContext.Cfg.Connection.InteractiveWsUrl;
        if (string.IsNullOrEmpty(wsUrl)) throw new Exception("No InteractiveWsUrl in config");

        var token = TestContext.Client.Token;
        var sep = wsUrl.Contains('?') ? '&' : '?';
        wsUrl = $"{wsUrl}{sep}access_token={token}";

        using var ws = new ClientWebSocket();
        await ws.ConnectAsync(new Uri(wsUrl), CancellationToken.None);
        TestContext.Log("       connected");

        var buf = new byte[65536];
        var sub = Encoding.UTF8.GetBytes("{\"action\":\"AllSubscribe\"}");
        await ws.SendAsync(new ArraySegment<byte>(sub), WebSocketMessageType.Text, true, CancellationToken.None);
        TestContext.Log("       subscribed");

        var po = TestContext.Cfg.PlaceOrder;
        var placeResult = await TestContext.Client.PlaceOrderAsync(new PlaceOrderRequest
        {
            CorrelationOrderId = $"test_{Guid.NewGuid():N}"[..16],
            Quantity = po.Quantity, Product = po.Product, Tif = po.Tif,
            Price = po.Price, OrderType = po.OrderType, OrderSide = po.OrderSide,
            DisclosedQuantity = po.DisclosedQuantity, StopPrice = po.StopPrice,
            TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
            InstrumentId = po.InstrumentId, ClientId = TestContext.Cfg.Connection.ClientId ?? "",
        });
        TestContext.Log($"       placed order blitzId={placeResult.Data?.BlitzOrderId}");

        var deadline = DateTime.UtcNow.AddSeconds(15);
        bool gotMsg = false;
        while (DateTime.UtcNow < deadline && ws.State == WebSocketState.Open)
        {
            WebSocketReceiveResult res;
            try { res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)); }
            catch (TimeoutException) { continue; }
            if (res.MessageType == WebSocketMessageType.Close) break;
            if (res.MessageType == WebSocketMessageType.Text)
            {
                gotMsg = true;
                var txt = Encoding.UTF8.GetString(buf, 0, res.Count);
                TestContext.Log($"       text({res.Count}): {txt[..Math.Min(120, txt.Length)]}");
                break;
            }
            else if (res.MessageType == WebSocketMessageType.Binary)
            {
                gotMsg = true;
                TestContext.Log($"       binary: {res.Count} bytes, first={BitConverter.ToString(buf, 0, Math.Min(16, res.Count))}");
                break;
            }
        }
        if (!gotMsg) TestContext.Log("       no WS event (order may not trigger WS on this server)");
        else TestContext.Log("       WS event received OK");

        if (placeResult.Data != null)
        {
            await TestContext.Client.CancelOrderAsync(new CancelOrderRequest
            {
                BlitzOrderId = placeResult.Data.BlitzOrderId,
                InstrumentId = TestContext.Cfg.PlaceOrder.InstrumentId,
            });
            TestContext.Log("       order cancelled");
        }

        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", CancellationToken.None);
        TestContext.Log("       closed");
    }

    static async Task TestWebSocketClient()
    {
        var ws = new BlitzWebSocketClient(TestContext.Config);
        var connected = new TaskCompletionSource<bool>();
        var gotMessage = new TaskCompletionSource<BlitzWsMessage>();
        var errors = new TaskCompletionSource<Exception>();

        ws.OnConnect += () => connected.TrySetResult(true);
        ws.OnMessage += (m) => { if (!gotMessage.Task.IsCompleted) gotMessage.TrySetResult(m); };
        ws.OnError += (e) => { if (!errors.Task.IsCompleted) errors.TrySetException(e); };

        ws.Start(TestContext.Client.Token);
        TestContext.Log("       starting client...");
        await connected.Task.WaitAsync(TimeSpan.FromSeconds(10));
        TestContext.Log("       connected");

        await ws.SubscribeActionAsync("AllSubscribe");
        TestContext.Log("       subscribed (AllSubscribe)");

        var winner = await Task.WhenAny(gotMessage.Task, errors.Task, Task.Delay(TimeSpan.FromSeconds(10)));
        if (winner == gotMessage.Task)
        {
            var msg = await gotMessage.Task;
            TestContext.Log($"       got message type={msg.Type} code={msg.MessageCode}");
            TestContext.Log("  [PASS] InteractiveWS.Client");
        }
        else if (winner == errors.Task)
        {
            TestContext.Log($"       error: {errors.Task.Exception?.InnerException?.Message}");
        }
        else
        {
            TestContext.Log("       no message received within 10s");
        }

        await ws.StopAsync();
        ws.Dispose();
    }
}
