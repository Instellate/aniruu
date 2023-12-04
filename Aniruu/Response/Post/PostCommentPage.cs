namespace Aniruu.Response.Post;

public class PostCommentPage
{
    public IEnumerable<PostComment> Comments { get; init; }
    public long Total { get; init; }

    public PostCommentPage(IEnumerable<PostComment> comments, long total)
    {
        this.Comments = comments;
        this.Total = total;
    }
}
