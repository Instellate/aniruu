namespace Aniruu.Database.Entities;

public class Tag
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public required TagType Type { get; init; }
}
