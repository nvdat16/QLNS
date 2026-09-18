using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Users;

/// <summary>users.status — an account is either usable or kept for its audit trail.</summary>
public enum UserAccountStatus
{
    Active,
    Disabled
}

public static class UserAccountStatusNames
{
    public const string Active = "active";
    public const string Disabled = "disabled";

    public static string ToContract(this UserAccountStatus status) => status switch
    {
        UserAccountStatus.Active => Active,
        UserAccountStatus.Disabled => Disabled,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out UserAccountStatus status)
    {
        switch (value)
        {
            case Active:
                status = UserAccountStatus.Active;
                return true;
            case Disabled:
                status = UserAccountStatus.Disabled;
                return true;
            default:
                status = default;
                return false;
        }
    }
}

/// <summary>Identity row of <c>users</c> plus the employee it is linked to, if any.</summary>
public sealed record UserAccount(
    long Id,
    string ExternalSubject,
    string Email,
    string DisplayName,
    UserAccountStatus Status,
    long? EmployeeId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version)
{
    public const int DisplayNameMaxLength = 255;
    public const int ExternalSubjectMaxLength = 255;

    /// <summary>
    /// users.external_subject of a locally provisioned account. The prefix keeps local accounts and
    /// subjects issued by a future external provider in separate namespaces of the same unique column.
    /// </summary>
    public const string LocalSubjectPrefix = "local|";

    public static string LocalSubject(string normalizedEmail) => LocalSubjectPrefix + normalizedEmail;
}

/// <summary>
/// An account as the administration API shows it: identity, role grants and the state of its credential.
/// The password hash itself never leaves the data layer.
/// </summary>
public sealed record UserAccountView(
    UserAccount Account,
    IReadOnlyList<RoleGrant> Grants,
    bool HasCredential,
    bool MustChangePassword,
    DateTimeOffset? PasswordUpdatedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LockedUntil)
{
    /// <summary>Widest data scope the grants add up to — the same computation sign-in uses.</summary>
    public CoreHrDataScope DataScope => new IdentityAuthorization(Grants, new HashSet<string>(StringComparer.Ordinal)).DataScope;
}

/// <summary>One row of <c>roles</c> with the permissions attached to it.</summary>
public sealed record RoleCatalogEntry(
    string Code,
    string Name,
    string? Description,
    bool IsAssignable,
    IReadOnlyList<string> Permissions);
