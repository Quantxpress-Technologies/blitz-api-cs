using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BlitzConnect.Services;
using Google.Protobuf;

namespace BlitzConnect.MarketData;

public class MarketDataWebSocket : IDisposable
{
    private readonly string _wsUrl;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private readonly HashSet<long> _subscribedInstruments = [];
    private int _reconnectDelay = 1000;

    public event Action<long, double>? OnLtp;
    public event Action<MarketDataMessageBase>? OnMessage;
    public event Action<TickDataMessage>? OnTick;
    public event Action<MarketDepthMessage>? OnMarketDepth;
    public event Action<TouchLineDataMessage>? OnTouchline;
    public event Action<IndexDataMessage>? OnIndex;
    public event Action<IndexDataListMessage>? OnIndexList;
    public event Action<IncrementalUpdateMessage>? OnIncremental;
    public event Action<string>? OnError;
    public event Action? OnConnected;
    public event Action<int, string>? OnDisconnected;

    public MarketDataWebSocket(string wsUrl)
    {
        _wsUrl = wsUrl;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws = new ClientWebSocket();

        await _ws.ConnectAsync(new Uri(_wsUrl), _cts.Token);

        OnConnected?.Invoke();
        _reconnectDelay = 1000;
        _receiveLoop = ReceiveLoopAsync(_cts.Token);
    }

    public async Task SubscribeAsync(IEnumerable<long> instrumentIds, CancellationToken ct = default)
    {
        var ids = instrumentIds.ToList();
        foreach (var id in ids) _subscribedInstruments.Add(id);
        await SendSubscriptionAsync("subscribe", ids, ct);
    }

    public async Task UnsubscribeAsync(IEnumerable<long> instrumentIds, CancellationToken ct = default)
    {
        var ids = instrumentIds.ToList();
        foreach (var id in ids) _subscribedInstruments.Remove(id);
        await SendSubscriptionAsync("unsubscribe", ids, ct);
    }

    private async Task SendSubscriptionAsync(string action, List<long> ids, CancellationToken ct)
    {
        var msg = JsonSerializer.Serialize(new
        {
            action,
            instrumentIds = ids,
        });
        await SendAsync(msg, ct);
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch { }
        }
        _ws?.Dispose();
        _ws = null;
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _ws?.Dispose();
    }

    private async Task SendAsync(string message, CancellationToken ct)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(message);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[65536];

        while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            try
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleDisconnect((int)(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure), result.CloseStatusDescription ?? "");
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    using var ms = new MemoryStream();
                    ms.Write(buffer, 0, result.Count);
                    while (!result.EndOfMessage)
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        ms.Write(buffer, 0, result.Count);
                    }
                    ProcessBinaryMessage(ms.ToArray());
                }
                else if (result.MessageType == WebSocketMessageType.Text)
                {
                    var sb = new StringBuilder(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    while (!result.EndOfMessage)
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                    ProcessTextMessage(sb.ToString());
                }
            }
            catch (OperationCanceledException) { return; }
            catch (WebSocketException ex)
            {
                await HandleDisconnect(1006, ex.Message);
                return;
            }
        }
    }

    private void ProcessBinaryMessage(byte[] data)
    {
        try
        {
            var msg = new MarketDataMessageBase();
            msg.MergeFrom(data);

            OnMessage?.Invoke(msg);

            switch (msg.SubtypeCase)
            {
                case MarketDataMessageBase.SubtypeOneofCase.TickDataMessage:
                    var tick = msg.TickDataMessage;
                    OnTick?.Invoke(tick);
                    if (tick.LTP > 0)
                        OnLtp?.Invoke((long)tick.InstrumentID, tick.LTP);
                    break;

                case MarketDataMessageBase.SubtypeOneofCase.TouchLineDataMessage:
                    var touch = msg.TouchLineDataMessage;
                    OnTouchline?.Invoke(touch);
                    if (touch.LTP > 0)
                        OnLtp?.Invoke((long)touch.InstrumentID, touch.LTP);
                    break;

                case MarketDataMessageBase.SubtypeOneofCase.MarketDepthMessage:
                    var depth = msg.MarketDepthMessage;
                    OnMarketDepth?.Invoke(depth);
                    if (depth.LTP > 0)
                        OnLtp?.Invoke((long)depth.InstrumentID, depth.LTP);
                    break;

                case MarketDataMessageBase.SubtypeOneofCase.IndexDataMessage:
                    OnIndex?.Invoke(msg.IndexDataMessage);
                    break;

                case MarketDataMessageBase.SubtypeOneofCase.IndexDataListMessage:
                    OnIndexList?.Invoke(msg.IndexDataListMessage);
                    break;

                case MarketDataMessageBase.SubtypeOneofCase.IncrementalUpdateMessage:
                    OnIncremental?.Invoke(msg.IncrementalUpdateMessage);
                    break;

                case MarketDataMessageBase.SubtypeOneofCase.TickData:
                    var td = msg.TickData;
                    if (td.LTP > 0)
                        OnLtp?.Invoke((long)td.InstrumentID, td.LTP);
                    break;
            }
        }
        catch (Exception ex)
        {
            OnError?.Invoke($"Protobuf parse error: {ex.Message}");
        }
    }

    private void ProcessTextMessage(string raw)
    {
        // Try base64-decoded protobuf first (sERVER MD WS sends base64 protobuf as text)
        byte[]? decoded = null;
        try
        {
            decoded = Convert.FromBase64String(raw);
        }
        catch (FormatException) { }

        if (decoded != null)
        {
            ProcessBinaryMessage(decoded);
            return;
        }

        // Fallback: try JSON parsing
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("type", out var typeProp))
            {
                var type = typeProp.GetString();
                if (type == "error" && root.TryGetProperty("message", out var errProp))
                    OnError?.Invoke(errProp.GetString() ?? raw);
            }
        }
        catch (JsonException) { }
    }

    private async Task HandleDisconnect(int closeCode, string closeReason)
    {
        OnDisconnected?.Invoke(closeCode, closeReason);

        while (_cts?.IsCancellationRequested != true)
        {
            try
            {
                await Task.Delay(_reconnectDelay, _cts?.Token ?? CancellationToken.None);
                _reconnectDelay = Math.Min(_reconnectDelay * 2, 30_000);

                await ConnectAsync(_cts?.Token ?? CancellationToken.None);

                if (_subscribedInstruments.Count > 0)
                    await SubscribeAsync(_subscribedInstruments, CancellationToken.None);

                return;
            }
            catch (OperationCanceledException) { return; }
            catch { }
        }
    }
}
