using Aniruu.Database.Entities;

namespace Aniruu.Utility;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizationAttribute : Attribute
{
    public UserPermission Permission { get; init; }

    public AuthorizationAttribute(UserPermission permission)
        => this.Permission = permission;
}
