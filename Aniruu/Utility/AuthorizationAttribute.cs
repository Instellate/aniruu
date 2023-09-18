using Aniruu.Database.Entities;

namespace Aniruu.Utility;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AuthorizationAttribute : Attribute
{
    public UserPermission Permission { get; }

    public AuthorizationAttribute(UserPermission permission)
        => this.Permission = permission;

    public AuthorizationAttribute()
        => this.Permission = 0;
}
