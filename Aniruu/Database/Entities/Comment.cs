namespace Aniruu.Database.Entities;

public class Comment
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Content { get; set; }
    public required long UserId { get; set; } 
    public User User { get; init; } = null!;
    public required long PostId { get; set; }
    public Post Post { get; init; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
