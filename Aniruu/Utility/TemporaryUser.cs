using Aniruu.Database.Entities;

namespace Aniruu.Utility;

public class TemporaryUser
{
    public required string Email { get; init; }
    public required UserConnectionType Type { get; init; }
}
