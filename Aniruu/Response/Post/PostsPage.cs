namespace Aniruu.Response.Post;

public class PostsPage
{
    /// <summary>
    /// The amount of pages this query has
    /// </summary>
    public required long Total { get; set; }
    
    public required IEnumerable<PostResponse> Posts { get; set; }
}
