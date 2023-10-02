namespace Aniruu.Database.Entities;

[Flags]
public enum UserPermission
{
    CreatePost = 1 << 0,
    // This is for editing any post
    EditPost = 1 << 1,
    DeletePost = 1 << 2,
    BanUser = 1 << 3,
    RemoveUser = 1 << 4,
    ChangeImportance = 1 << 5,
    CreateComment = 1 << 6,
    DeleteComment = 1 << 7,
}
