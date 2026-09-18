using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Users;
using Qlns.DataAccess.Modules.Contracts.Contracts;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Identity.Shared;

namespace Qlns.DataAccess.Modules.Identity.Users;

/// <summary>
/// PostgreSQL persistence of account administration (ADM-02). Writes are conditional on users.version, so a
/// stale ETag loses instead of overwriting a concurrent change, and each one commits its audit row in the
/// same transaction. Role grants are replaced wholesale (delete-then-insert) because the natural key of
/// user_roles is the triple (role, scope type, scope id) — a diff would be harder to read for no gain.
/// </summary>
public sealed class UserAccountRepository(QlnsDbContext dbContext) : IUserAccountRepository
{
    private const string EntityType = UserAccountAuditActions.EntityType;
    // A duplicate e-mail also duplicates external_subject (it is derived from the e-mail), and PostgreSQL
    // reports whichever unique index it happens to check first. Both are treated as "e-mail already taken",
    // otherwise the race between the pre-check and the insert would surface as a 500.
    private static readonly string[] EmailConstraints = ["users_email_key", "users_external_subject_key"];

    public async Task<PagedResult<UserAccountView>> SearchAsync(
        UserAccountSearchQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var users = dbContext.Set<UserEntity>().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            users = users.Where(user =>
                EF.Functions.ILike(user.Email, pattern) || EF.Functions.ILike(user.DisplayName, pattern));
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            users = users.Where(user => user.Status == statusValue);
        }

        if (!string.IsNullOrWhiteSpace(query.RoleCode))
        {
            var roleCode = query.RoleCode.Trim();
            users = users.Where(user => dbContext.Set<UserRoleEntity>()
                .Any(grant => grant.UserId == user.Id && grant.RoleCode == roleCode));
        }

        var totalItems = await users.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<UserAccountView>.Empty(query.Page);
        }

        var page = await users
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        var items = await HydrateAsync(page, cancellationToken);
        return new PagedResult<UserAccountView>(items, query.Page.Page, query.Page.PageSize, totalItems);
    }

    public async Task<UserAccountView?> GetAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<UserEntity>()
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var views = await HydrateAsync([user], cancellationToken);
        return views[0];
    }

    public Task<bool> EmailTakenAsync(string normalizedEmail, long? exceptUserId, CancellationToken cancellationToken) =>
        dbContext.Set<UserEntity>()
            .AsNoTracking()
            .AnyAsync(user => user.Email == normalizedEmail && (exceptUserId == null || user.Id != exceptUserId), cancellationToken);

    public async Task<IReadOnlyList<RoleCatalogEntry>> GetRoleCatalogAsync(CancellationToken cancellationToken)
    {
        var roles = await dbContext.Set<RoleEntity>()
            .AsNoTracking()
            .OrderBy(role => role.Code)
            .ToListAsync(cancellationToken);

        var permissions = await dbContext.Set<RolePermissionEntity>()
            .AsNoTracking()
            .OrderBy(permission => permission.Permission)
            .ToListAsync(cancellationToken);

        var byRole = permissions
            .GroupBy(permission => permission.RoleCode, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<string>)group.Select(permission => permission.Permission).ToList(),
                StringComparer.Ordinal);

        return roles
            .Select(role => new RoleCatalogEntry(
                role.Code,
                role.Name,
                role.Description,
                role.IsAssignable,
                byRole.TryGetValue(role.Code, out var granted) ? granted : []))
            .ToList();
    }

    public async Task<IReadOnlyList<long>> FindMissingDepartmentsAsync(
        IReadOnlyCollection<long> departmentIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(departmentIds);

        if (departmentIds.Count == 0)
        {
            return [];
        }

        var existing = await dbContext.Set<DepartmentEntity>()
            .AsNoTracking()
            .Where(department => departmentIds.Contains(department.Id))
            .Select(department => department.Id)
            .ToListAsync(cancellationToken);

        return departmentIds.Except(existing).ToList();
    }

    public Task<EmployeeLink?> GetEmployeeLinkAsync(long employeeId, CancellationToken cancellationToken) =>
        dbContext.Set<EmployeeEntity>()
            .AsNoTracking()
            .Where(employee => employee.Id == employeeId)
            .Select(employee => new EmployeeLink(employee.Id, employee.UserId))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<long> CreateAsync(NewUserAccount account, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var user = new UserEntity
        {
            ExternalSubject = account.ExternalSubject,
            Email = account.Email,
            DisplayName = account.DisplayName,
            Status = UserAccountStatusNames.Active,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.CreatedAt,
            Version = 1
        };

        dbContext.Set<UserEntity>().Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsEmailConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new CoreHrBusinessRuleException(
                UserAccountService.EmailTakenCode, $"Another account already uses the e-mail {account.Email}.");
        }

        dbContext.Set<UserCredentialEntity>().Add(new UserCredentialEntity
        {
            UserId = user.Id,
            PasswordHash = account.PasswordHash,
            PasswordAlgorithm = account.PasswordAlgorithm,
            // A secret chosen by an administrator is known to someone other than its owner, so the account
            // is forced to replace it before the first full session is granted.
            MustChangePassword = true,
            PasswordUpdatedAt = account.CreatedAt,
            FailedAttempts = 0,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.CreatedAt,
            Version = 1
        });

        foreach (var grant in account.Grants)
        {
            dbContext.Set<UserRoleEntity>().Add(ToEntity(user.Id, grant, actor, account.CreatedAt));
        }

        if (account.EmployeeId is { } employeeId)
        {
            var linked = await dbContext.Set<EmployeeEntity>()
                .Where(employee => employee.Id == employeeId && employee.UserId == null)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(employee => employee.UserId, user.Id)
                        .SetProperty(employee => employee.UpdatedAt, account.CreatedAt)
                        .SetProperty(employee => employee.Version, employee => employee.Version + 1),
                    cancellationToken);

            if (linked != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                throw new CoreHrConcurrencyConflictException("employee link");
            }
        }

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            actor.UserId,
            UserAccountAuditActions.Create,
            EntityType,
            IdentityAudit.EntityId(user.Id),
            before: null,
            after: new
            {
                email = account.Email,
                displayName = account.DisplayName,
                employeeId = account.EmployeeId,
                roles = account.Grants.Select(grant => new
                {
                    roleCode = grant.RoleCode,
                    dataScopeType = grant.ScopeType.ToContract(),
                    dataScopeId = grant.ScopeId
                })
            },
            actor.CorrelationId,
            account.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return user.Id;
    }

    public async Task<bool> UpdateAsync(
        long userId,
        long expectedVersion,
        string normalizedEmail,
        string displayName,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        int rows;
        try
        {
            rows = await dbContext.Set<UserEntity>()
                .Where(user => user.Id == userId && user.Version == expectedVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(user => user.Email, normalizedEmail)
                        .SetProperty(user => user.DisplayName, displayName)
                        .SetProperty(user => user.UpdatedAt, now)
                        .SetProperty(user => user.Version, user => user.Version + 1),
                    cancellationToken);
        }
        catch (PostgresException exception) when (IsEmailConflict(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new CoreHrBusinessRuleException(
                UserAccountService.EmailTakenCode, $"Another account already uses the e-mail {normalizedEmail}.");
        }

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            actor.UserId,
            UserAccountAuditActions.Update,
            EntityType,
            IdentityAudit.EntityId(userId),
            new { version = expectedVersion },
            new { version = expectedVersion + 1, changedFields },
            actor.CorrelationId,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetStatusAsync(
        long userId,
        long expectedVersion,
        UserAccountStatus status,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Set<UserEntity>()
            .Where(user => user.Id == userId && user.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.Status, status.ToContract())
                    .SetProperty(user => user.UpdatedAt, now)
                    .SetProperty(user => user.Version, user => user.Version + 1),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Disabling must end the open sessions too; the access token still lives until it expires, which is
        // the documented upper bound of the stateless design.
        var revoked = status == UserAccountStatus.Disabled
            ? await RevokeActiveTokensAsync(userId, RefreshTokenRevocationReasons.AccountDisabled, now, cancellationToken)
            : 0;

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            actor.UserId,
            status == UserAccountStatus.Disabled ? UserAccountAuditActions.Disable : UserAccountAuditActions.Enable,
            EntityType,
            IdentityAudit.EntityId(userId),
            new { version = expectedVersion },
            new { status = status.ToContract(), version = expectedVersion + 1, revokedRefreshTokens = revoked },
            actor.CorrelationId,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(
        long userId,
        string passwordHash,
        string algorithm,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Set<UserCredentialEntity>()
            .Where(entity => entity.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entity => entity.PasswordHash, passwordHash)
                    .SetProperty(entity => entity.PasswordAlgorithm, algorithm)
                    .SetProperty(entity => entity.MustChangePassword, true)
                    .SetProperty(entity => entity.PasswordUpdatedAt, now)
                    .SetProperty(entity => entity.FailedAttempts, 0)
                    .SetProperty(entity => entity.LockedUntil, (DateTimeOffset?)null)
                    .SetProperty(entity => entity.UpdatedAt, now)
                    .SetProperty(entity => entity.Version, entity => entity.Version + 1),
                cancellationToken);

        if (rows != 1)
        {
            // No credential row yet: the account was provisioned without a local password. Create one.
            if (!await dbContext.Set<UserEntity>().AnyAsync(user => user.Id == userId, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            dbContext.Set<UserCredentialEntity>().Add(new UserCredentialEntity
            {
                UserId = userId,
                PasswordHash = passwordHash,
                PasswordAlgorithm = algorithm,
                MustChangePassword = true,
                PasswordUpdatedAt = now,
                FailedAttempts = 0,
                CreatedAt = now,
                UpdatedAt = now,
                Version = 1
            });
        }

        var revoked = await RevokeActiveTokensAsync(
            userId, RefreshTokenRevocationReasons.RevokedByAdmin, now, cancellationToken);

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            actor.UserId,
            UserAccountAuditActions.PasswordReset,
            EntityType,
            IdentityAudit.EntityId(userId),
            before: null,
            after: new { algorithm, mustChangePassword = true, revokedRefreshTokens = revoked },
            actor.CorrelationId,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ReplaceRoleGrantsAsync(
        long userId,
        long expectedVersion,
        IReadOnlyList<RoleGrant> grants,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grants);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // The version guard on users is what serializes two administrators editing the same account, even
        // though the rows being replaced live in user_roles.
        var rows = await dbContext.Set<UserEntity>()
            .Where(user => user.Id == userId && user.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(user => user.UpdatedAt, now)
                    .SetProperty(user => user.Version, user => user.Version + 1),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var before = await dbContext.Set<UserRoleEntity>()
            .AsNoTracking()
            .Where(grant => grant.UserId == userId)
            .Select(grant => new { grant.RoleCode, grant.DataScopeType, grant.DataScopeId })
            .ToListAsync(cancellationToken);

        await dbContext.Set<UserRoleEntity>()
            .Where(grant => grant.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        foreach (var grant in grants)
        {
            dbContext.Set<UserRoleEntity>().Add(ToEntity(userId, grant, actor, now));
        }

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            actor.UserId,
            UserAccountAuditActions.RoleGrantsReplace,
            EntityType,
            IdentityAudit.EntityId(userId),
            new
            {
                version = expectedVersion,
                roles = before.Select(grant => new
                {
                    roleCode = grant.RoleCode,
                    dataScopeType = grant.DataScopeType,
                    dataScopeId = grant.DataScopeId
                })
            },
            new
            {
                version = expectedVersion + 1,
                roles = grants.Select(grant => new
                {
                    roleCode = grant.RoleCode,
                    dataScopeType = grant.ScopeType.ToContract(),
                    dataScopeId = grant.ScopeId
                })
            },
            actor.CorrelationId,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static bool IsEmailConflict(Exception exception) =>
        EmailConstraints.Any(constraint => UniqueViolation.IsOn(exception, constraint));

    private Task<int> RevokeActiveTokensAsync(
        long userId,
        string reason,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        dbContext.Set<RefreshTokenEntity>()
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedReason, reason),
                cancellationToken);

    private async Task<IReadOnlyList<UserAccountView>> HydrateAsync(
        IReadOnlyList<UserEntity> users,
        CancellationToken cancellationToken)
    {
        var userIds = users.Select(user => user.Id).ToList();

        var grants = await dbContext.Set<UserRoleEntity>()
            .AsNoTracking()
            .Where(grant => userIds.Contains(grant.UserId))
            .OrderBy(grant => grant.RoleCode)
            .ThenBy(grant => grant.DataScopeId)
            .ToListAsync(cancellationToken);

        var credentials = await dbContext.Set<UserCredentialEntity>()
            .AsNoTracking()
            .Where(credential => userIds.Contains(credential.UserId))
            .Select(credential => new
            {
                credential.UserId,
                credential.MustChangePassword,
                credential.PasswordUpdatedAt,
                credential.LastLoginAt,
                credential.LockedUntil
            })
            .ToListAsync(cancellationToken);

        var employees = await dbContext.Set<EmployeeEntity>()
            .AsNoTracking()
            .Where(employee => employee.UserId != null && userIds.Contains(employee.UserId!.Value))
            .Select(employee => new { employee.Id, UserId = employee.UserId!.Value })
            .ToListAsync(cancellationToken);

        var grantsByUser = grants.GroupBy(grant => grant.UserId).ToDictionary(group => group.Key, group => group.ToList());
        var credentialByUser = credentials.ToDictionary(credential => credential.UserId);
        var employeeByUser = employees.ToDictionary(employee => employee.UserId, employee => employee.Id);

        return users
            .Select(user =>
            {
                UserAccountStatusNames.TryParseContract(user.Status, out var status);
                credentialByUser.TryGetValue(user.Id, out var credential);
                var userGrants = grantsByUser.TryGetValue(user.Id, out var rows) ? rows : [];

                return new UserAccountView(
                    new UserAccount(
                        user.Id,
                        user.ExternalSubject,
                        user.Email,
                        user.DisplayName,
                        status,
                        employeeByUser.TryGetValue(user.Id, out var employeeId) ? employeeId : null,
                        user.CreatedAt,
                        user.UpdatedAt,
                        user.Version),
                    userGrants
                        .Select(grant => DataScopeTypeNames.TryParseContract(grant.DataScopeType, out var scope)
                            ? new RoleGrant(grant.RoleCode, scope, grant.DataScopeId)
                            : null)
                        .Where(grant => grant is not null)
                        .Select(grant => grant!)
                        .ToList(),
                    HasCredential: credential is not null,
                    MustChangePassword: credential?.MustChangePassword ?? false,
                    PasswordUpdatedAt: credential?.PasswordUpdatedAt,
                    LastLoginAt: credential?.LastLoginAt,
                    LockedUntil: credential?.LockedUntil);
            })
            .ToList();
    }

    private static UserRoleEntity ToEntity(long userId, RoleGrant grant, CoreHrActor actor, DateTimeOffset now) => new()
    {
        UserId = userId,
        RoleCode = grant.RoleCode,
        DataScopeType = grant.ScopeType.ToContract(),
        DataScopeId = grant.ScopeId,
        GrantedAt = now,
        GrantedBy = actor.UserId > 0 ? actor.UserId : null
    };
}
