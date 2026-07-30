using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BlitzConnect.Common;
using BlitzConnect.Common.Models;

namespace BlitzConnect.Interactive;

public class BlitzWsMessage
{
    public string? Type { get; set; }
    public int? MessageCode { get; set; }
    public JsonElement? Body { get; set; }
}

public class BlitzWebSocketClient : IDisposable
{
    private readonly BlitzConfig _config;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private string? _token;
    private int _reconnectDelay = 1000;
    private bool _closing;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly Dictionary<string, int[]> ActionCodes = new()
    {
        ["OrderSubscribe"] = [70000],
        ["OrderUnsubscribe"] = [70000],
        ["StatisticSubscribe"] = [50000],
        ["StatisticUnsubscribe"] = [50000],
        ["StrategyStatisticSubscribe"] = [80000],
        ["StrategyStatisticUnsubscribe"] = [80000],
        ["InstrumentStatisticSubscribe"] = [90000],
        ["InstrumentStatisticUnsubscribe"] = [90000],
        ["AllSubscribe"] = [50000, 70000, 80000, 90000],
        ["AllUnsubscribe"] = [50000, 70000, 80000, 90000],
    };

    public event Action<BlitzWsMessage>? OnMessage;
    public event Action? OnConnect;
    public event Action<Exception>? OnError;
    public event Action<int, string>? OnClose;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public BlitzWebSocketClient(BlitzConfig config)
    {
        _config = config;
    }

    public void Start(string? token = null)
    {
        _token = token;
        _closing = false;
        _cts = new CancellationTokenSource();
        _ = RunLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        _closing = true;
        _cts?.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            try { await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None); }
            catch { }
        }
        _ws?.Dispose();
        _ws = null;
    }

    public async Task SubscribeActionAsync(string action)
    {
        if (!ActionCodes.ContainsKey(action))
        {
            OnError?.Invoke(new ArgumentException($"Unknown action: {action}"));
            return;
        }
        await SendJsonAsync(new { action });
    }

    public async Task SubscribeAsync(List<long> instrumentIds)
    {
        await SendJsonAsync(new { action = "subscribe", instrumentIds });
    }

    public void Dispose()
    {
        _closing = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _ws?.Dispose();
    }

    private async Task SendJsonAsync(object data)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(data, JsonOptions);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private async Task RunLoopAsync(CancellationToken ct)
    {
        while (!_closing && !ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAsync(ct);
                OnConnect?.Invoke();

                _reconnectDelay = 1000;
                await ReceiveLoopAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex) when (!_closing)
            {
                OnError?.Invoke(ex);
                await Task.Delay(_reconnectDelay, ct);
                _reconnectDelay = Math.Min(_reconnectDelay * 2, 30_000);
            }
        }
    }

    private async Task ConnectAsync(CancellationToken ct)
    {
        var wsUrl = _config.InteractiveWsUrl;
        if (!string.IsNullOrEmpty(_token))
        {
            var sep = wsUrl.Contains('?') ? '&' : '?';
            wsUrl = $"{wsUrl}{sep}access_token={_token}";
        }

        _ws?.Dispose();
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri(wsUrl), ct);
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        var messageBuffer = new StringBuilder();

        while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                var status = (int)(result.CloseStatus ?? WebSocketCloseStatus.NormalClosure);
                var desc = result.CloseStatusDescription ?? "";
                OnClose?.Invoke(status, desc);
                return;
            }

            if (result.MessageType == WebSocketMessageType.Binary)
                continue;

            messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

            if (result.EndOfMessage)
            {
                ProcessMessage(messageBuffer.ToString());
                messageBuffer.Clear();
            }
        }
    }

    private void ProcessMessage(string raw)
    {
        raw = raw.Trim();
        if (raw.Length == 0 || (raw[0] != '{' && raw[0] != '['))
            return;

        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            var msg = new BlitzWsMessage();

            if (root.TryGetProperty("MessageCode", out var mc))
                msg.MessageCode = mc.GetInt32();
            else if (root.TryGetProperty("type", out var t))
                msg.Type = t.GetString();

            msg.Body = root.Clone();

            OnMessage?.Invoke(msg);
        }
        catch (JsonException)
        {
        }
    }
}
