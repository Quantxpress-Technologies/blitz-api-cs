namespace BlitzConnect.Common;

public class BlitzConfig
{
    public string AuthBaseUrl { get; init; } = "https://uat.bull8.ai:7443/api_gateway/v1";
    public string OrderBaseUrl { get; init; } = "https://uat.bull8.ai:7443/api_interactive/api/v1/";
    public string MarketDataApiUrl { get; init; } = "https://uat.bull8.ai:7443/md-api";
    public string InteractiveWsUrl { get; init; } = "wss://uat.bull8.ai:7443/api_interactive/ws";
    public string MarketDataWsUrl { get; init; } = "wss://uat.bull8.ai:7443/md-streaming/ws";
    public string InstrumentBaseUrl { get; init; } = "https://uat.bull8.ai:7443/v1/api/instruments";
    public string InstrumentGzUrl { get; init; } = "https://uat.bull8.ai:7443/v1/api/instruments/gz/download";
    public string AppKey { get; init; } = "";
    public string UserId { get; init; } = "";
    public string ClientId { get; init; } = "";
    public int RequestTimeoutSeconds { get; init; } = 30;
    public int HeartbeatIntervalSeconds { get; init; } = 30;

    /// <summary>
    /// Skips TLS certificate validation. Defaults to true because the UAT host
    /// presents a certificate from a private CA that is not in the machine trust
    /// store. Set to false in production.
    /// </summary>
    public bool SkipCertificateValidation { get; init; } = true;
}
