using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BlitzConnect.Models;

namespace BlitzConnect.Services;

public class MarketDataWebSocket : IDisposable
{
    private readonly string _wsUrl;
    private readonly Func<Task<string?>> _getToken;
    private ClientWebSocket? _ws;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private readonly HashSet<long> _subscribedLtp = [];
    private readonly HashSet<long> _subscribedQuotes = [];
    private int _reconnectDelay = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public event Action<long, double>? OnLtp;
    public event Action<MarketQuoteEntry>? OnQuote;
    public event Action<string>? OnError;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public MarketDataWebSocket(string wsUrl, Func<Task<string?>> getToken)
    {
        _wsUrl = wsUrl;
        _getToken = getToken;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ws = new ClientWebSocket();

        await _ws.ConnectAsync(new Uri(_wsUrl), _cts.Token);

        var token = await _getToken();
        if (token is not null)
            await SendAsync(token, _cts.Token);

        OnConnected?.Invoke();
        _reconnectDelay = 1000;
        _receiveLoop = ReceiveLoopAsync(_cts.Token);
    }

    public async Task SubscribeLtpAsync(IEnumerable<long> instrumentIds, CancellationToken ct = default)
    {
        var ids = instrumentIds.ToList();
        foreach (var id in ids) _subscribedLtp.Add(id);

        var msg = JsonSerializer.Serialize(new
        {
            type = "subscribe",
            channel = "ltp",
            instrumentIds = ids,
        });
        await SendAsync(msg, ct);
    }

    public async Task SubscribeQuoteAsync(IEnumerable<long> instrumentIds, CancellationToken ct = default)
    {
        var ids = instrumentIds.ToList();
        foreach (var id in ids) _subscribedQuotes.Add(id);

        var msg = JsonSerializer.Serialize(new
        {
            type = "subscribe",
            channel = "quote",
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
        var buffer = new byte[8192];
        var messageBuffer = new StringBuilder();

        while (!ct.IsCancellationRequested && _ws?.State == WebSocketState.Open)
        {
            try
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleDisconnect();
                    return;
                }

                messageBuffer.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));

                if (result.EndOfMessage)
                {
                    ProcessMessage(messageBuffer.ToString());
                    messageBuffer.Clear();
                }
            }
            catch (OperationCanceledException) { return; }
            catch (WebSocketException)
            {
                await HandleDisconnect();
                return;
            }
        }
    }

    private void ProcessMessage(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (!root.TryGetProperty("type", out var typeProp)) return;
            var type = typeProp.GetString();

            switch (type)
            {
                case "ltp":
                    if (root.TryGetProperty("data", out var ltpData))
                    {
                        var entry = ltpData.Deserialize<LtpEntry>(JsonOptions);
                        if (entry is not null)
                            OnLtp?.Invoke(entry.InstrumentId, entry.Ltp);
                    }
                    break;

                case "quote":
                    if (root.TryGetProperty("data", out var quoteData))
                    {
                        var entry = quoteData.Deserialize<MarketQuoteEntry>(JsonOptions);
                        if (entry is not null)
                            OnQuote?.Invoke(entry);
                    }
                    break;

                case "error":
                    OnError?.Invoke(raw);
                    break;
            }
        }
        catch (JsonException ex)
        {
            OnError?.Invoke($"Parse error: {ex.Message}\n{raw}");
        }
    }

    private async Task HandleDisconnect()
    {
        OnDisconnected?.Invoke();

        if (_cts?.IsCancellationRequested == true) return;

        await Task.Delay(_reconnectDelay);
        _reconnectDelay = Math.Min(_reconnectDelay * 2, 30_000);

        try
        {
            await ConnectAsync(_cts?.Token ?? CancellationToken.None);

            if (_subscribedLtp.Count > 0)
                await SubscribeLtpAsync(_subscribedLtp, CancellationToken.None);
            if (_subscribedQuotes.Count > 0)
                await SubscribeQuoteAsync(_subscribedQuotes, CancellationToken.None);
        }
        catch
        {
            _ = HandleDisconnect();
        }
    }
}
