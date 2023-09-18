namespace Aniruu.Response;

public enum ErrorCode
{
    InternalError,
    TooManyRequests,
    NameTooBig,
    NoUserAgent,
    NameAlreadyInUsage,
    NoTokenForClaimingName,
    NoSessionFound,
    NotAValidMediaType,
    Unauthorized,
    Forbidden,
    NotImplemented,
    NoPostFound
}
