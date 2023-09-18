namespace Aniruu.Utility.OAuth;

public class OAuth2GoogleResponse
{
    public required string AccessToken { get; init; }
    public required ulong ExpiresIn { get; init; }
    public required string RefreshToken { get; init; }
}
