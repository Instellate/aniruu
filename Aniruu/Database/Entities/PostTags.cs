namespace Aniruu.Database.Entities;

public class PostTags
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required Tag Tag { get; init; }
    public required Post Post { get; init; }
}
