namespace BlitzConnect.Common.Models;

public class BlitzApiResponse<T>
{
    public string Status { get; init; } = "";
    public T? Data { get; init; }
    public string? Message { get; init; }
}

public class LoginData
{
    public string AccessToken { get; init; } = "";
}

public class LoginResponse : BlitzApiResponse<LoginData> { }
