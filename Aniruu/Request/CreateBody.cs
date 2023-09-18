using Aniruu.Database.Entities;

namespace Aniruu.Request;

public class CreateBody
{
    public required string Tags { get; init; }
    public required PostRating Rating { get; init; }
    public string? Source { get; init; }
}
