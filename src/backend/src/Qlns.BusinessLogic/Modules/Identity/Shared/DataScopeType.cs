using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Shared;

/// <summary>user_roles.data_scope_type — the breadth of data a role grant is valid for.</summary>
public enum DataScopeType
{
    /// <summary>Only the actor's own employee record.</summary>
    Self,

    /// <summary>One department; the grant carries its id in <c>data_scope_id</c>.</summary>
    Department,

    /// <summary>The whole organization; <c>data_scope_id</c> is 0.</summary>
    Organization
}

public static class DataScopeTypeNames
{
    public const string Self = "self";
    public const string Department = "department";
    public const string Organization = "organization";

    public static string ToContract(this DataScopeType type) => type switch
    {
        DataScopeType.Self => Self,
        DataScopeType.Department => Department,
        DataScopeType.Organization => Organization,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static bool TryParseContract(string? value, out DataScopeType type)
    {
        switch (value)
        {
            case Self:
                type = DataScopeType.Self;
                return true;
            case Department:
                type = DataScopeType.Department;
                return true;
            case Organization:
                type = DataScopeType.Organization;
                return true;
            default:
                type = default;
                return false;
        }
    }
}

/// <summary>
/// One row of user_roles: a role granted to a user within a data scope. <c>ck_user_roles_scope</c> in the
/// schema requires a positive department id for <see cref="DataScopeType.Department"/> and 0 otherwise.
/// </summary>
public sealed record RoleGrant(string RoleCode, DataScopeType ScopeType, long ScopeId)
{
    public static RoleGrant Organization(string roleCode) => new(roleCode, DataScopeType.Organization, 0);

    public static RoleGrant Self(string roleCode) => new(roleCode, DataScopeType.Self, 0);

    public static RoleGrant Department(string roleCode, long departmentId) =>
        new(roleCode, DataScopeType.Department, departmentId);

    /// <summary>Validates the scope/id pairing the way the database constraint does.</summary>
    public bool IsWellFormed => ScopeType == DataScopeType.Department ? ScopeId > 0 : ScopeId == 0;
}

/// <summary>
/// Effective permissions and data scope of a user, resolved from user_roles ⋈ role_permissions.
/// A user with no grant authenticates successfully but can call no business endpoint (deny by default).
/// </summary>
public sealed record IdentityAuthorization(
    IReadOnlyList<RoleGrant> Grants,
    IReadOnlySet<string> Permissions)
{
    public static IdentityAuthorization None { get; } = new([], new HashSet<string>(StringComparer.Ordinal));

    public IReadOnlyList<string> RoleCodes => Grants
        .Select(grant => grant.RoleCode)
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList();

    /// <summary>
    /// Widest scope any grant gives: organization-wide beats the union of departments, which beats self.
    /// </summary>
    public CoreHrDataScope DataScope
    {
        get
        {
            if (Grants.Any(grant => grant.ScopeType == DataScopeType.Organization))
            {
                return CoreHrDataScope.Organization;
            }

            var departments = Grants
                .Where(grant => grant.ScopeType == DataScopeType.Department && grant.ScopeId > 0)
                .Select(grant => grant.ScopeId)
                .ToHashSet();

            return departments.Count == 0 ? CoreHrDataScope.Self : new CoreHrDataScope(false, departments);
        }
    }
}
