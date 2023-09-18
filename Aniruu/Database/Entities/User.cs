using Aniruu.Utility;

namespace Aniruu.Database.Entities;

public class User
{
    public long Id { get; init; }
    public required string Username { get; init; }
    public required string PrimaryEmail { get; set; }

    public UserPermission Permission { get; set; }
        = (UserPermission)UserRoles.Normal;

    public int Importance { get; set; } = 0;
}
