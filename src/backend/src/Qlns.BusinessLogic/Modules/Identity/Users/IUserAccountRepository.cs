using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Users;

/// <summary>
/// Persistence contract of account administration (ADM-02). Writes are conditional on users.version and
/// pair the change with its audit row in one transaction; false means the caller's ETag was stale.
/// </summary>
public interface IUserAccountRepository
{
    Task<PagedResult<UserAccountView>> SearchAsync(UserAccountSearchQuery query, CancellationToken cancellationToken);

    Task<UserAccountView?> GetAsync(long userId, CancellationToken cancellationToken);

    /// <summary>True when another account already uses the normalized e-mail.</summary>
    Task<bool> EmailTakenAsync(string normalizedEmail, long? exceptUserId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RoleCatalogEntry>> GetRoleCatalogAsync(CancellationToken cancellationToken);

    /// <summary>Department ids of <paramref name="departmentIds"/> that do not exist.</summary>
    Task<IReadOnlyList<long>> FindMissingDepartmentsAsync(
        IReadOnlyCollection<long> departmentIds,
        CancellationToken cancellationToken);

    /// <summary>
    /// Link state of an employee: null when the employee does not exist, otherwise the user it is already
    /// linked to (null when free).
    /// </summary>
    Task<EmployeeLink?> GetEmployeeLinkAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts users, user_credentials and user_roles, links employees.user_id when an employee was given,
    /// and writes the audit row. Returns the new user id.
    /// </summary>
    Task<long> CreateAsync(NewUserAccount account, CoreHrActor actor, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(
        long userId,
        long expectedVersion,
        string normalizedEmail,
        string displayName,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Flips users.status. Disabling also revokes every refresh token of the account, so an open session
    /// cannot outlive the decision by more than one access-token lifetime.
    /// </summary>
    Task<bool> SetStatusAsync(
        long userId,
        long expectedVersion,
        UserAccountStatus status,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Replaces the credential with an administrator-chosen secret, forces a change at next sign-in,
    /// clears the lockout counters and revokes every refresh token of the account.
    /// </summary>
    Task<bool> ResetPasswordAsync(
        long userId,
        string passwordHash,
        string algorithm,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Replaces the whole set of user_roles rows of the account (delete-then-insert in one transaction).</summary>
    Task<bool> ReplaceRoleGrantsAsync(
        long userId,
        long expectedVersion,
        IReadOnlyList<RoleGrant> grants,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>Whether an employee exists and which user account it already belongs to.</summary>
public sealed record EmployeeLink(long EmployeeId, long? UserId);
