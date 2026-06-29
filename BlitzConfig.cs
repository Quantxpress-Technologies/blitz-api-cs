namespace BlitzConnect;

public class BlitzConfig
{
    public string BaseUrl { get; init; } = "http://uat.bull8.ai:7443";
    public string AuthBaseUrl { get; init; } = "http://uat.bull8.ai:7443/api_gateway/v1";
    public string OrderBaseUrl { get; init; } = "http://uat.bull8.ai:7443/v1/api";
    public string AppKey { get; init; } = "";
    public string UserId { get; init; } = "";
    public string ClientId { get; init; } = "";
}
