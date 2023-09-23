namespace Aniruu.Response.Post;

public class PostComment
{
    public required string Content { get; init; }
    public required PostAuthorResponse Author { get; init; }
    public required long CreatedAt { get; set; }
}
