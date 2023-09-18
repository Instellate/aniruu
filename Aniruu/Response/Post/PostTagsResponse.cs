using Aniruu.Database.Entities;

namespace Aniruu.Response.Post;

public class PostTagsResponse
{
    public TagType Type { get; init; }
    public string Name { get; init; }

    public PostTagsResponse(TagType type, string name)
    {
        this.Type = type;
        this.Name = name;
    }
}
