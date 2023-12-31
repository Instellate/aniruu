namespace Aniruu.Response;

public class Error
{
    public int StatusCode { get; }
    public ErrorCode ErrorCode { get; }

    public Error(int statusCode, ErrorCode errorCode)
    {
        this.StatusCode = statusCode;
        this.ErrorCode = errorCode;
    }
}
