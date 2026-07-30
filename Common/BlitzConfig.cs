namespace BlitzConnect.Common;

public class BlitzConfig
{
    public string AuthBaseUrl { get; init; } = "https://uat.bull8.ai:7443/api_gateway/v1";
    public string OrderBaseUrl { get; init; } = "https://uat.bull8.ai:7443/v1/api";
    public string MarketDataApiUrl { get; init; } = "https://uat.bull8.ai:7443/md-api";
    public string InteractiveWsUrl { get; init; } = "wss://uat.bull8.ai:7443/ws";
    public string MarketDataWsUrl { get; init; } = "wss://uat.bull8.ai:7443/md-streaming/ws?key=";
    public string InstrumentGzUrl { get; init; } = "";
    public string AppKey { get; init; } = "";
    public string UserId { get; init; } = "";
    public string ClientId { get; init; } = "";
    public int RequestTimeoutSeconds { get; init; } = 30;
}
