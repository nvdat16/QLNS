using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.Api.Modules.Identity.Shared;

/// <summary>
/// Endpoint-level authorization policies of account administration (ADM-02). Sign-in, refresh, sign-out,
/// <c>GET /auth/me</c> and the self-service password change carry no policy: the first three are anonymous
/// by nature and the last two are available to every authenticated actor.
/// </summary>
public static class IdentityPolicies
{
    public const string UserRead = "IdentityUserRead";
    public const string UserManage = "IdentityUserManage";
    public const string RoleRead = "IdentityRoleRead";

    public static void AddIdentityPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(UserRead, policy => policy.RequireClaim("permission",
            IdentityPermissions.UserRead, IdentityPermissions.UserManage));
        options.AddPolicy(UserManage, policy => policy.RequireClaim("permission", IdentityPermissions.UserManage));
        options.AddPolicy(RoleRead, policy => policy.RequireClaim("permission",
            IdentityPermissions.RoleRead, IdentityPermissions.UserManage));
    }
}
