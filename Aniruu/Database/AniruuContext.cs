using Aniruu.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aniruu.Database;

public class AniruuContext : DbContext
{
    public AniruuContext(DbContextOptions<AniruuContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; init; } = null!;
    public DbSet<UserConnection> Connections { get; init; } = null!;
    public DbSet<Session> Sessions { get; init; } = null!;
    public DbSet<Post> Posts { get; init; } = null!;

    public DbSet<Tag> Tags { get; init; } = null!;

    public DbSet<PostTags> PostTags { get; init; } = null!;
}
