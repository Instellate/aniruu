namespace Aniruu.Database.Entities;

public class Session
{
    public Guid Id { get; init; }
    public UserConnectionType Type { get; init; }
    public User User { get; init; } = null!;
    public long UserId { get; init; }
    public required string UserAgent { get; init; }
}
