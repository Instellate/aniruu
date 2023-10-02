using Aniruu.Database.Entities;

namespace Aniruu.Response;

public class UserProfile
{
    public long Id { get; }
    public string Username { get; }
    public UserPermission? Permission { get; }
    public List<long> Posts { get; }

    public UserProfile(User user, List<long>? posts = null, bool includePerm = false)
    {
        this.Id = user.Id;
        this.Username = user.Username;
        this.Permission = includePerm ? user.Permission : null;
        this.Posts = posts ?? new List<long>();
    }
}
