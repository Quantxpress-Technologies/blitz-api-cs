# AGENTS.md

## Project

**BlitzConnect** — .NET 8 class library wrapping a REST API (Bull8/QuantXpress trading platform).
**BlitzConnectTest** — console app for manual integration testing (subdirectory of the library project).

## Build & run

```bash
dotnet build BlitzConnect.sln
dotnet run --project BlitzConnectTest
```

Test runner loads credentials from `.env` at repo root (first), with `BlitzConnectTest/test-config.xml` as fallback. Most test cases in `Program.cs` are commented out — uncomment to exercise specific endpoints.

## Project structure

- **`Services/BlitzApiClient.cs`** — implements `IBlitzApiClient`, single client class with all REST API logic.
- **`Services/IBlitzApiClient.cs`** — interface for testability and mocking.
- **`Services/MarketDataWebSocket.cs`** — real-time WebSocket streaming client for LTP/quotes with auto-reconnect.
- **`Models/`** — typed response wrappers; all use `[JsonPropertyName("camelCase")]`.
- **`BlitzConfig.cs`** — config POCO with defaults pointing to `uat.bull8.ai:7443`. Two base URLs: `BaseUrl` (market data) vs `OrderBaseUrl` (trading).

## Critical gotchas

- **BlitzConnectTest is a subdirectory of BlitzConnect** → SDK globbing picks up `BlitzConnectTest/*.cs` into the library. The `.csproj` has `<Compile Remove="BlitzConnectTest/**" />`. Do not remove.
- **Test project uses `ProjectReference`** (not raw DLL). Reverting to DLL ref causes `MSB3245` / `CS0006`.
- **SSL verification is disabled** (`ServerCertificateCustomValidationCallback = (_,_,_,_) => true`).
- **`dotnet clean` may leave stale `obj/`** — `rm -rf obj bin BlitzConnectTest/obj BlitzConnectTest/bin` if you see duplicate attribute errors.

## Auth & requests

`LoginAsync()` gets a JWT stored in-memory. `RequestAsync<T>` auto-calls `LoginAsync()` if no token exists, and auto-retries once on 401.

- **Market data** (`MarketRequestAsync`) — auto-selects GET/POST based on body being null. Uses `BlitzConfig.BaseUrl`.
- **Trading** (`TradingRequestAsync`) — explicit `HttpMethod`. Uses `BlitzConfig.OrderBaseUrl`.
- **`CancelOrderAsync`** builds query string manually (HTTP DELETE cannot carry a body).
- **`ModifyOrderAsync`** — set `Symbol = null` explicitly (passing a non-null symbol may cause `"order price is out of price band"`).
- **`GetOptionChainRawAsync`** returns `null` on failure instead of throwing.
- **`GetHistoricalDataAsync`** uses `accept: "*/*"`.
- **All async methods accept `CancellationToken ct = default`** — callers can pass it; non-breaking default.
- **`MarketDataWebSocket`** — separate non-REST client. Constructor takes WebSocket URL and a `Func<Task<string?>>` token provider. Subscribes via `SubscribeLtpAsync`/`SubscribeQuoteAsync`. Fires events `OnLtp`, `OnQuote`, `OnConnected`, `OnDisconnected`, `OnError`. Auto-reconnects with exponential backoff (1s–30s) and restores subscriptions.

## API response model

All responses wrap through `BlitzApiResponse<T>` (`status`, `data`, `message`).
`OrdersResponse` / `PositionsResponse` / `TradesResponse` / `OptionChainResponse` / `LtpResponse` / `MarketQuoteResponse` / `DepthResponse` are typed subclasses.
