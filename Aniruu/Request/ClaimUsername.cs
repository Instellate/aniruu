namespace Aniruu.Request;

public class ClaimUsername
{
    public required string Name { get; init; }
    public required Guid TemporaryToken { get; init; }
}
