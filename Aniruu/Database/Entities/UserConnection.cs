namespace Aniruu.Database.Entities;

public class UserConnection
{
    public required Guid Id { get; init; }
    public required UserConnectionType Type { get; init; }
    public User User { get; init; } = null!;
    public long UserId { get; init; }
    public required string Email { get; init; } = null!;
}
