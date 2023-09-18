using Aniruu.Database.Entities;

namespace Aniruu.Utility.OAuth;

public class OAuth2Info
{
    public required string Email { get; init; }
    public UserConnectionType Type { get; init; }

    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }

    public required DateTimeOffset AccessTokenExpiration { get; init; }
    public required DateTimeOffset RefreshTokenExpiration { get; init; }
}
