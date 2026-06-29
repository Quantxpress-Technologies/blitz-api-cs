using System.Text.Json.Serialization;

namespace BlitzConnect.Models;

public class BlitzApiResponse<T>
{
    [JsonPropertyName("status")]
    public string Status { get; init; } = "";

    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

public class LoginData
{
    [JsonPropertyName("accessToken")]
    public string AccessToken { get; init; } = "";
}

public class LoginResponse : BlitzApiResponse<LoginData> { }
