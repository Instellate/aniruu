using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aniruu.Database;

public class DesignTimeFactory : IDesignTimeDbContextFactory<AniruuContext>
{
    public AniruuContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<AniruuContext> builder = new();
        builder.UseNpgsql("Host=localhost:5432;Database=aniruu;Username=postgres");
            // .UseSnakeCaseNamingConvention(); TODO: Enable this when it works in .NET 8.

        return new AniruuContext(builder.Options);
    }
}
