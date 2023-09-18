namespace Aniruu.Utility.OAuth;

public class OAuth2DiscordResponse
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public required long ExpiresIn { get; init; }
    public required string Scope { get; init; }
    public required string RefreshToken { get; init; }
}
