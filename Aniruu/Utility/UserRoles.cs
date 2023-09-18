using Aniruu.Database.Entities;

namespace Aniruu.Utility;

/// <summary>
/// Default permissions for roles.
/// You can change these to your own permissions if you are deploying your own instance.
/// </summary>
public enum UserRoles
{
    Normal = UserPermission.CreatePost,
    Trusted = Normal | UserPermission.EditPost,
    Moderator = Trusted | UserPermission.DeletePost | UserPermission.BanUser,
    Admin = Moderator | UserPermission.RemoveUser,
    Owner = Admin | UserPermission.ChangeImportance
}
