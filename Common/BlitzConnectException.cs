using System.Text.Json;
using BlitzConnect.Common.Models;

namespace BlitzConnect.Common;

public class BlitzConnectException : Exception
{
    public int HttpStatusCode { get; }
    public string? RawResponseBody { get; }
    public BlitzApiResponse<object>? ApiError { get; }

    public BlitzConnectException(int httpStatusCode, string message, string? rawResponseBody = null)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
        RawResponseBody = rawResponseBody;
        ApiError = TryParseError(rawResponseBody);
    }

    public BlitzConnectException(int httpStatusCode, string message, Exception inner, string? rawResponseBody = null)
        : base(message, inner)
    {
        HttpStatusCode = httpStatusCode;
        RawResponseBody = rawResponseBody;
        ApiError = TryParseError(rawResponseBody);
    }

    private static BlitzApiResponse<object>? TryParseError(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        try { return JsonSerializer.Deserialize<BlitzApiResponse<object>>(raw); }
        catch { return null; }
    }
}
