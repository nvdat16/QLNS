using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Users;

namespace Qlns.BusinessLogic.UnitTests.Modules.Identity.Users;

/// <summary>
/// In-memory <see cref="IUserAccountRepository"/> holding a single account. Writes only record what the
/// service decided; the version guard is simulated with <see cref="WriteSucceeds"/>.
/// </summary>
internal sealed class FakeUserAccountRepository(UserAccountView? account = null) : IUserAccountRepository
{
    public UserAccountView? Account { get; set; } = account;
    public bool WriteSucceeds { get; set; } = true;
    public bool EmailTaken { get; set; }
    public EmployeeLink? EmployeeLink { get; set; }
    public IReadOnlyList<long> MissingDepartments { get; set; } = [];

    public IReadOnlyList<RoleCatalogEntry> Catalogue { get; set; } =
    [
        new("ROLE_HR_MGR", "HR Manager", null, true, ["corehr.employee.read"]),
        new("ROLE_LINE_MGR", "Line Manager", null, true, ["corehr.probation.read"]),
        new("ROLE_LEGACY", "Retired role", null, false, [])
    ];

    public List<NewUserAccount> Created { get; } = [];
    public List<(long UserId, long Version, string Email, string DisplayName, IReadOnlyList<string> Changed)> Updates { get; } = [];
    public List<(long UserId, UserAccountStatus Status)> StatusChanges { get; } = [];
    public List<(long UserId, string Hash)> PasswordResets { get; } = [];
    public List<(long UserId, IReadOnlyList<RoleGrant> Grants)> GrantReplacements { get; } = [];

    public Task<PagedResult<UserAccountView>> SearchAsync(UserAccountSearchQuery query, CancellationToken cancellationToken) =>
        Task.FromResult(Account is null
            ? PagedResult<UserAccountView>.Empty(query.Page)
            : new PagedResult<UserAccountView>([Account], query.Page.Page, query.Page.PageSize, 1));

    public Task<UserAccountView?> GetAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult(Account is not null && Account.Account.Id == userId ? Account : null);

    public Task<bool> EmailTakenAsync(string normalizedEmail, long? exceptUserId, CancellationToken cancellationToken) =>
        Task.FromResult(EmailTaken);

    public Task<IReadOnlyList<RoleCatalogEntry>> GetRoleCatalogAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Catalogue);

    public Task<IReadOnlyList<long>> FindMissingDepartmentsAsync(
        IReadOnlyCollection<long> departmentIds,
        CancellationToken cancellationToken) =>
        Task.FromResult(MissingDepartments);

    public Task<EmployeeLink?> GetEmployeeLinkAsync(long employeeId, CancellationToken cancellationToken) =>
        Task.FromResult(EmployeeLink);

    public Task<long> CreateAsync(NewUserAccount account, CoreHrActor actor, CancellationToken cancellationToken)
    {
        Created.Add(account);
        Account = UserAccountTestData.View(
            id: 42, email: account.Email, displayName: account.DisplayName, grants: account.Grants);
        return Task.FromResult(42L);
    }

    public Task<bool> UpdateAsync(
        long userId,
        long expectedVersion,
        string normalizedEmail,
        string displayName,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        Updates.Add((userId, expectedVersion, normalizedEmail, displayName, changedFields));
        return Task.FromResult(WriteSucceeds);
    }

    public Task<bool> SetStatusAsync(
        long userId,
        long expectedVersion,
        UserAccountStatus status,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        StatusChanges.Add((userId, status));
        return Task.FromResult(WriteSucceeds);
    }

    public Task<bool> ResetPasswordAsync(
        long userId,
        string passwordHash,
        string algorithm,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        PasswordResets.Add((userId, passwordHash));
        return Task.FromResult(WriteSucceeds);
    }

    public Task<bool> ReplaceRoleGrantsAsync(
        long userId,
        long expectedVersion,
        IReadOnlyList<RoleGrant> grants,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        GrantReplacements.Add((userId, grants));
        return Task.FromResult(WriteSucceeds);
    }
}

internal static class UserAccountTestData
{
    public static UserAccountView View(
        long id = 7,
        string email = "new.user@qlns.local",
        string displayName = "Nguyen Van New",
        long version = 3,
        UserAccountStatus status = UserAccountStatus.Active,
        IReadOnlyList<RoleGrant>? grants = null) =>
        new(
            new UserAccount(
                id,
                UserAccount.LocalSubject(email),
                email,
                displayName,
                status,
                EmployeeId: null,
                CreatedAt: IdentityTestData.Now.AddDays(-30),
                UpdatedAt: IdentityTestData.Now.AddDays(-1),
                Version: version),
            grants ?? [RoleGrant.Organization("ROLE_HR_MGR")],
            HasCredential: true,
            MustChangePassword: false,
            PasswordUpdatedAt: IdentityTestData.Now.AddDays(-30),
            LastLoginAt: IdentityTestData.Now.AddHours(-2),
            LockedUntil: null);
}
