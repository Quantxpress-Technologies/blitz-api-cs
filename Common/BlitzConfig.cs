namespace BlitzConnect.Common;

public class BlitzConfig
{
public string AuthBaseUrl { get; init; } = "http://blitztrader.com/api_gateway/v1";
public string OrderBaseUrl { get; init; } = "http://blitztrader.com/api_interactive/api/v1";
public string MarketDataApiUrl { get; init; } = "http://blitztrader.com/md-api";
public string InteractiveWsUrl { get; init; } = "ws://blitztrader.com/api_interactive/ws";
public string MarketDataWsUrl { get; init; } = "ws://blitztrader.com/md-streaming/ws?key=";
public string InstrumentGzUrl { get; init; } = "http://blitztrader.com/v1/api/instruments/gz/download";
    public string AppKey { get; init; } = "";
    public string UserId { get; init; } = "";
    public string ClientId { get; init; } = "";
    public int RequestTimeoutSeconds { get; init; } = 30;
}
