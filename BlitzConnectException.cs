namespace BlitzConnect;

public class BlitzConnectException : Exception
{
    public int HttpStatusCode { get; }

    public BlitzConnectException(int httpStatusCode, string message)
        : base(message)
    {
        HttpStatusCode = httpStatusCode;
    }

    public BlitzConnectException(int httpStatusCode, string message, Exception inner)
        : base(message, inner)
    {
        HttpStatusCode = httpStatusCode;
    }
}
