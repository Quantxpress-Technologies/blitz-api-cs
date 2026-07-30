# BlitzConnect .NET SDK

[![NuGet](https://img.shields.io/badge/nuget-v1.0.0-blue)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-blue)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()

The official .NET client for communicating with the [BlitzConnect API](https://quantxpress.com/docs/blitz-api).

BlitzConnect is a set of REST-like APIs that expose many capabilities required to build a complete investment and trading platform. Execute orders in real time, manage user portfolio, stream live market data (WebSockets), and more, with the simple HTTP API collection.

[QuantXpress](https://quantxpress.com) (c) 2024. Licensed under the MIT License.

## Documentation

- [BlitzConnect API Reference](https://quantxpress.com/docs/blitz-api/sdk/blitzconnect-api/)
- [Interactive SDK](https://quantxpress.com/docs/blitz-api/sdk/blitzconnect-api/orders/)
- [Market Data SDK](https://quantxpress.com/docs/blitz-api/sdk/blitzconnect-api/ltp/)
- [WebSocket Streaming](https://quantxpress.com/docs/blitz-api/sdk/blitzconnect-api/websocket/)
- [Response structure & Errors](https://quantxpress.com/docs/blitz-api/sdk/blitzconnect/response-structure/)
- [REST API Reference](https://quantxpress.com/docs/blitz-api/api/API_Structure/)

## Installing

```xml
<PackageReference Include="Google.Protobuf" Version="3.30.2" />
<PackageReference Include="System.Net.Http.WinHttpHandler" Version="10.0.10" />
```

Or clone and build:

```
dotnet build BlitzConnect.csproj
```

Requires .NET 8.0.

## Interactive API usage

```csharp
using BlitzConnect.Common;
using BlitzConnect.Common.Models;
using BlitzConnect.Interactive;

// Configure
var config = new BlitzConfig
{
    AppKey = "your_app_key",
    UserId = "your_user_id",
    ClientId = "your_client_id",
};

// Create client (login happens automatically on first request)
var client = new BlitzInteractiveApiClient(config);

// Place an order
try
{
    var order = new PlaceOrderRequest
    {
        CorrelationOrderId = $"order_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
        Quantity = 1,
        Product = "MIS",
        Tif = "GFD",
        Price = 11,
        OrderType = "LIMIT",
        OrderSide = "BUY",
        DisclosedQuantity = 0,
        StopPrice = 0,
        ClientId = config.ClientId,
        TifGtdDate = DateTime.Now.AddDays(7).ToString("yyyy-MM-dd"),
        InstrumentId = 110010000014366,
        Symbol = "NSECM|IDEA",
    };
    var resp = await client.PlaceOrderAsync(order);
    Console.WriteLine($"Order placed. ID: {resp.Data?.BlitzOrderId}");
}
catch (Exception e)
{
    Console.WriteLine($"Order placement failed: {e.Message}");
}

// Modify order
var modifyResp = await client.ModifyOrderAsync(new ModifyOrderRequest
{
    BlitzOrderId = 12345,
    ModifiedOrderQuantity = 2,
    Price = 10.5,
    OrderType = "LIMIT",
    Tif = "GFD",
    DisclosedQuantity = 0,
    StopPrice = 0,
    TifGtdDate = DateTime.Now.ToString("yyyy-MM-dd"),
    InstrumentId = 110010000014366,
    Symbol = "NSECM|IDEA",
});

// Cancel order
var cancelResp = await client.CancelOrderAsync(new CancelOrderRequest
{
    BlitzOrderId = 12345,
    InstrumentId = 110010000014366,
});

// Fetch orders, positions, trades
var orders = await client.GetOrdersAsync();
var openOrders = await client.GetOpenOrdersAsync();
var positions = await client.GetPositionsAsync();
var trades = await client.GetTradesAsync();
var orderById = await client.GetOrderByIdAsync(blitzOrderId: 24091124420000098);
var tradeById = await client.GetTradeByIdAsync(tradeId: 12345);

// Send signals
var signals = new List<SignalRequest>();
signals.Add(new SignalRequest
{
    ID = "abc",
    SourceStrategy = "Bull8.DiamondX1",
    DestinationStrategy = dest,
    SL = "3000",
    SourceSID = $"Bull8_SINGLE_{dest}",
    InstanceRunningMode = "Started",
    GlobalAction = "Signal",
    Instruments = new List<SignalInstrument>
    {
        new()
        {
            ExchangeSegment = "NSEFO",
            InstrumentName = instrument_name1,
            Action = "BUY",
            Lot = "1",
            TimeStamp = base_time.ToString("dd-MM-yyyy HH:mm:ss"),
            InfoText = $"{option_type1} Entry Signal {strike1}",
        }
    },
});
var signalResp = await client.SendSignalsAsync(signals);
```

## Market Data API usage

```csharp
using BlitzConnect.Common;
using BlitzConnect.MarketData;

var config = new BlitzConfig
{
    AppKey = "your_app_key",
    UserId = "your_user_id",
};
var md = new BlitzMarketDataApiClient(config);

// Instrument lookup
var instr = await md.GetInstrumentDetailsAsync(id: 110010000002885);
var instrBySymbol = await md.GetInstrumentDetailsAsync(symbol: "NSECM|RELIANCE");

// Live market data
var ltp = await md.GetLtpAsync(new List<long> { 110010000002885 });
foreach (var (k, v) in ltp.Data ?? new())
    Console.WriteLine($"{k}: LTP={v.Ltp}");

var quote = await md.GetMarketQuoteAsync(new List<long> { 110010000002885 });
foreach (var (k, v) in quote.Data ?? new())
    Console.WriteLine($"{k}: LTP={v.Ltp} OI={v.Oi} Vol={v.Vtt}");

var optionChain = await md.GetOptionChainAsync("NIFTY", "2026-09-01");
Console.WriteLine($"Spot={optionChain.Data?.SpotPrice} Expiry={optionChain.Data?.ExpiryDate} Chains={optionChain.Data?.Chains.Count}");

var historical = await md.GetHistoricalDataAsync("RELIANCE", "D");
Console.WriteLine($"Got {historical.Count} candles");
```

Refer to the [BlitzConnect API Reference](https://quantxpress.com/docs/blitz-api/sdk/blitzconnect-api/) for the complete list of supported methods.

## Interactive WebSocket usage

```csharp
using BlitzConnect.Common;
using BlitzConnect.Interactive;

var config = new BlitzConfig
{
    AppKey = "your_app_key",
    UserId = "your_user_id",
};

var ws = new BlitzWebSocketClient(config);

ws.OnConnect += () => Console.WriteLine("Connected");
ws.OnMessage += (msg) =>
{
    Console.WriteLine($"Code: {msg.MessageCode} Type: {msg.Type} Body: {msg.Body}");
};
ws.OnError += (ex) => Console.WriteLine($"Error: {ex.Message}");
ws.OnClose += (code, reason) => ws.StopAsync();

ws.Start();

// Subscribe to all order and statistics updates
await ws.SubscribeActionAsync("AllSubscribe");

// Subscribe to specific instruments
await ws.SubscribeAsync(new List<long> { 738561, 5633 });
```

## Market Data WebSocket usage

Market data ticks are streamed as **protobuf** (Protocol Buffers). The SDK decodes them and fires typed events:

```csharp
using BlitzConnect.MarketData;

// Append access token to the WS URL
var url = $"wss://uat.bull8.ai:7443/md-streaming/ws?key={accessToken}";
var ws = new MarketDataWebSocket(url);

ws.OnConnected += () => Console.WriteLine("Connected");
ws.OnMarketDepth += (depth) =>
{
    Console.WriteLine($"{depth.InstrumentName} LTP: {depth.LTP} " +
        $"Bid: {depth.BestBidLevel?.FirstOrDefault()?.Price} " +
        $"Ask: {depth.BestAskLevel?.FirstOrDefault()?.Price}");
};
ws.OnLtp += (instrumentId, ltp) =>
{
    Console.WriteLine($"Instrument {instrumentId} LTP: {ltp}");
};
ws.OnMessage += (msg) =>
{
    // msg is MarketDataMessageBase (protobuf)
    // msg.SubtypeCase tells you which type:
    //   TickDataMessage, TouchLineDataMessage, MarketDepthMessage, IndexDataMessage, TickData, etc.
    var json = Google.Protobuf.JsonFormatter.Default.Format(msg);
    Console.WriteLine(json);
};
ws.OnError += (err) => Console.WriteLine($"Error: {err}");
ws.OnDisconnected += (code, reason) => Console.WriteLine($"Disconnected: {code} {reason}");

await ws.ConnectAsync();
await ws.SubscribeLtpAsync(new List<long> { 110010002000001, 110010000002885 });

// Unsubscribe
await ws.UnsubscribeLtpAsync(new List<long> { 110010000002885 });
```

## Instrument Manager

Downloads gzipped JSON (~140K instruments), decompresses and caches in memory.

```csharp
using BlitzConnect.Common;

var manager = new BlitzInstrumentManager();
await manager.LoadInstrumentsAsync(config.InstrumentGzUrl);

if (manager.TryGetInstrumentId("NSECM|RELIANCE", out var id))
    Console.WriteLine($"ID: {id}");

if (manager.TryGetLotSize(id, out var lotSize))
    Console.WriteLine($"LotSize: {lotSize}");

Console.WriteLine($"Total: {manager.Count}");
```

## Response models

All API responses return typed wrappers:

```csharp
public class BlitzApiResponse<T>
{
    public string Status { get; init; } = "";     // "success" or "error"
    public T? Data { get; init; }                  // strongly-typed payload
    public string? Message { get; init; }          // response message
}

// Market data responses
public class LtpResponse : BlitzApiResponse<Dictionary<string, LtpEntry>> { }
public class MarketQuoteResponse : BlitzApiResponse<Dictionary<string, MarketQuoteEntry>> { }
public class OptionChainResponse : BlitzApiResponse<OptionChainData> { }

// Interactive responses
public class OrdersResponse    { public List<OrderEntry> Data { get; init; } = []; }
public class PositionsResponse { public List<JsonElement> Data { get; init; } = []; }
public class TradesResponse    { public List<JsonElement> Data { get; init; } = []; }
```

## Run tests

```bash
dotnet run --project BlitzConnectTest
```

Tests read credentials from `test-config.json` and write a timestamped log file (`blitz-test-{yyyyMMdd-HHmmss}.log`).

## Changelog

[Check release notes](https://github.com/your-org/blitzsdk/releases)
