namespace Aniruu.Database.Entities;

public class Post
{
    public long Id { get; init; }

    public required PostRating Rating { get; set; }
    public string? Source { get; set; }

    public required string Checksum { get; init; }
    public required string DefaultExtension { get; init; }

    public User User { get; init; } = null!;
    public long UserId { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public List<PostTags> Tags { get; init; } = new();
}
