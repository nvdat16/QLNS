namespace Qlns.BusinessLogic.Modules.Identity.Shared;

/// <summary>
/// Permission claim values of the Identity &amp; Access module (ADM). Only account administration is
/// permission-guarded: signing in, refreshing and changing one's own password are available to every
/// authenticated actor and therefore have no permission of their own.
/// </summary>
public static class IdentityPermissions
{
    public const string UserRead = "admin.user.read";
    public const string UserManage = "admin.user.manage";
    public const string RoleRead = "admin.role.read";
}
