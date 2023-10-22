using Aniruu.Database.Entities;

namespace Aniruu.Response.Post;

public class PostResponse
{
    public long Id { get; init; }
    public string Location { get; init; }
    public PostRating Rating { get; init; }
    public string? Source { get; init; }
    public List<PostTagsResponse> Tags { get; init; }
    public PostAuthorResponse CreatedBy { get; init; }
    public DateTime CreatedAt { get; init; }

    public PostResponse(Aniruu.Database.Entities.Post post)
    {
        Id = post.Id;
        Location = $"/api/post/{post.Id}";
        CreatedAt = post.CreatedAt;
        CreatedBy = new PostAuthorResponse(post.User.Id, post.User.Username);
        Rating = post.Rating;
        Source = post.Source;
        Tags = new List<PostTagsResponse>(post.Tags.Count);
        
        foreach (PostTags t in post.Tags)
        {
            this.Tags.Add(new PostTagsResponse(t.Tag.Type, t.Tag.Name));
        }
    }
}
