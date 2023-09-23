namespace Aniruu.Response.Post;

public class PostAuthorResponse
{
    public long Id { get; init; }
    public string Name { get; init; }

    public PostAuthorResponse(long id, string name)
    {
        this.Id = id;
        this.Name = name;
    }
    
}
