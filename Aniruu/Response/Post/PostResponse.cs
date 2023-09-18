using Aniruu.Database.Entities;

namespace Aniruu.Response.Post;

public class PostResponse
{
    public required long Id { get; init; }
    public required string Location { get; init; }
    public required PostRating Rating { get; init; }
    public string? Source { get; init; }
    public List<PostTagsResponse> Tags { get; init; } = new();
    public required PostAuthorResponse CreatedBy { get; init; }
    public required DateTime CreatedAt { get; init; }
}
