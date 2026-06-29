# AGENTS.md

## Project

**BlitzConnect** — .NET 9 class library wrapping a REST API (Bull8/QuantXpress trading platform).  
**BlitzConnectTest** — console app for manual integration testing.

## Build & run

```bash
dotnet build BlitzConnect.sln
dotnet run --project BlitzConnectTest
```

Test runner loads credentials from `../.env` (one dir above repo root). Most test cases are commented out — uncomment to exercise specific endpoints.

## Critical gotchas

- **BlitzConnectTest is a subdirectory of BlitzConnect** → SDK globbing picks up `BlitzConnectTest/*.cs` into the library project. The main `.csproj` has `<Compile Remove="BlitzConnectTest/**" />` to suppress this. Do not remove it.
- **Test project uses `ProjectReference`** (not raw DLL). If you see `MSB3245` or `CS0006`, the reference type was reverted.
- **SSL verification is disabled** (`ServerCertificateCustomValidationCallback = (_,_,_,_) => true`).

## Auth

`LoginAsync()` gets a JWT stored in-memory. `RequestAsync<T>` auto-calls `LoginAsync()` if no token exists, and auto-retries once on 401.

## Architecture

- **Market data** (`MarketRequestAsync`) — auto-selects GET/POST based on body being null. Base URL from `BlitzConfig.BaseUrl`.
- **Trading** (`TradingRequestAsync`) — explicit `HttpMethod`. Base URL from `BlitzConfig.OrderBaseUrl`.
- **CancelOrderAsync** builds query string manually (HTTP DELETE does not carry a body).
- **ModifyOrderAsync** — set `Symbol = null` explicitly (passing a non-null symbol string may cause `"order price is out of price band"`).
- All responses wrap through `BlitzApiResponse<T>` (fields: `status`, `data`, `message`).
- `OrdersResponse` / `PositionsResponse` / `TradesResponse` are typed subclasses of `BlitzApiResponse<T>`.

## Known issues

- `retried` variable bug in `RequestAsync<T>` (checked before being set) — auto-retry on 401 works anyway.
- CS8618 warnings on `SignalInstrument` / `SignalRequest` string props (all nullable warnings, pre-existing).
- `dotnet clean` may leave stale `obj/` files — `rm -rf obj bin BlitzConnectTest/obj BlitzConnectTest/bin` if you see duplicate attribute errors.

## Conventions

- `[JsonPropertyName("camelCase")]` on all model fields.
- `PropertyNameCaseInsensitive = true` JSON option.
- Console.WriteLine for request/response debug output throughout `BlitzApiClient.cs`.
