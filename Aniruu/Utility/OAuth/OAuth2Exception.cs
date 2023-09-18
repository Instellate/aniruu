namespace Aniruu.Utility.OAuth;

public sealed class OAuth2Exception : Exception
{
    public new Exception? InnerException { get; set; }

    public OAuth2Exception()
    {
    }

    public OAuth2Exception(string message) : base(message)
    {

    }

    public OAuth2Exception(Exception innerException)
    {
        this.InnerException = innerException;
    }
}

