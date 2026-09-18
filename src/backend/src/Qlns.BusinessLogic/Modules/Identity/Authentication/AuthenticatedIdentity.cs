using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// Everything a token needs to carry about the signed-in user. Built once per sign-in or refresh from
/// users ⋈ employees ⋈ user_roles ⋈ role_permissions, never from anything the client sent.
/// </summary>
public sealed record AuthenticatedIdentity(
    long UserId,
    long? EmployeeId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlySet<string> Permissions,
    CoreHrDataScope DataScope,
    bool PasswordChangeRequired)
{
    /// <summary>Value of the <c>data_scope</c> claim (see <c>CoreHrActorResolver</c>).</summary>
    public string DataScopeClaim =>
        DataScope.OrganizationWide ? DataScopeTypeNames.Organization
        : DataScope.DepartmentIds.Count > 0 ? DataScopeTypeNames.Department
        : DataScopeTypeNames.Self;

    /// <summary>
    /// The restricted identity issued while a password change is pending: no role, no permission and
    /// self scope, so the only thing the token can do is call the change-password endpoint.
    /// </summary>
    public static AuthenticatedIdentity PendingPasswordChange(long userId, long? employeeId, string email, string displayName) =>
        new(userId, employeeId, email, displayName, [], new HashSet<string>(StringComparer.Ordinal), CoreHrDataScope.Self, true);

    public static AuthenticatedIdentity From(UserSignInRecord record, IdentityAuthorization authorization) => new(
        record.UserId,
        record.EmployeeId,
        record.Email,
        record.DisplayName,
        authorization.RoleCodes,
        authorization.Permissions,
        authorization.DataScope,
        PasswordChangeRequired: false);
}

/// <summary>Result of a successful sign-in or refresh.</summary>
public sealed record AuthenticationSession(
    AuthenticatedIdentity Identity,
    IssuedAccessToken AccessToken,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAt);
