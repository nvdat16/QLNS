using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Users;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Identity.Shared;

namespace Qlns.DataAccess.Modules.Identity.Authentication;

/// <summary>
/// PostgreSQL persistence of sign-in (ADM-01). Every method that changes state opens one transaction and
/// commits the business row together with its audit row, so an authentication event is either fully
/// recorded or has not happened. Reads are <c>AsNoTracking</c>; writes go through <c>ExecuteUpdateAsync</c>
/// with a guard in the <c>WHERE</c> clause, which is what makes concurrent refreshes of the same token safe.
/// </summary>
public sealed class AuthenticationRepository(QlnsDbContext dbContext) : IAuthenticationRepository
{
    private const string EntityType = AuthenticationAuditActions.EntityType;
    private const string RefreshTokenEntityType = "refresh_token";

    public async Task<UserSignInRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<UserEntity>()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        return user is null ? null : await BuildAsync(user, cancellationToken);
    }

    public async Task<UserSignInRecord?> FindByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.Set<UserEntity>()
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : await BuildAsync(user, cancellationToken);
    }

    public async Task<IdentityAuthorization> GetAuthorizationAsync(long userId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<UserRoleEntity>()
            .AsNoTracking()
            .Where(grant => grant.UserId == userId)
            .Select(grant => new { grant.RoleCode, grant.DataScopeType, grant.DataScopeId })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return IdentityAuthorization.None;
        }

        // A row whose data_scope_type is not one of the three contract values would silently widen or narrow
        // the scope; it is dropped instead, which can only ever remove authority.
        var grants = rows
            .Select(row => DataScopeTypeNames.TryParseContract(row.DataScopeType, out var scope)
                ? new RoleGrant(row.RoleCode, scope, row.DataScopeId)
                : null)
            .Where(grant => grant is not null)
            .Select(grant => grant!)
            .ToList();

        var roleCodes = grants.Select(grant => grant.RoleCode).Distinct(StringComparer.Ordinal).ToList();
        var permissions = await dbContext.Set<RolePermissionEntity>()
            .AsNoTracking()
            .Where(permission => roleCodes.Contains(permission.RoleCode))
            .Select(permission => permission.Permission)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new IdentityAuthorization(grants, permissions.ToHashSet(StringComparer.Ordinal));
    }

    public async Task RecordFailedSignInAsync(SignInFailure failure, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(failure);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (failure.Credential is { } credential && failure.UserId is { } userId)
        {
            await dbContext.Set<UserCredentialEntity>()
                .Where(entity => entity.UserId == userId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(entity => entity.FailedAttempts, credential.FailedAttempts)
                        .SetProperty(entity => entity.LockedUntil, credential.LockedUntil)
                        .SetProperty(entity => entity.UpdatedAt, failure.OccurredAt)
                        .SetProperty(entity => entity.Version, entity => entity.Version + 1),
                    cancellationToken);
        }

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            failure.UserId,
            AuthenticationAuditActions.SignIn,
            EntityType,
            IdentityAudit.EntityId(failure.UserId),
            before: null,
            after: new
            {
                email = failure.Email,
                reason = failure.Code,
                failedAttempts = failure.Credential?.FailedAttempts,
                lockedUntil = failure.Credential?.LockedUntil,
                clientIp = failure.Context.ClientIp,
                userAgent = failure.Context.UserAgent
            },
            failure.Context.CorrelationId,
            failure.OccurredAt,
            IdentityAudit.Rejected));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RecordSuccessfulSignInAsync(
        long userId,
        NewRefreshToken? refreshToken,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Set<UserCredentialEntity>()
            .Where(entity => entity.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entity => entity.FailedAttempts, 0)
                    .SetProperty(entity => entity.LockedUntil, (DateTimeOffset?)null)
                    .SetProperty(entity => entity.LastLoginAt, now)
                    .SetProperty(entity => entity.UpdatedAt, now)
                    .SetProperty(entity => entity.Version, entity => entity.Version + 1),
                cancellationToken);

        if (refreshToken is not null)
        {
            dbContext.Set<RefreshTokenEntity>().Add(ToEntity(refreshToken));
        }

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            userId,
            AuthenticationAuditActions.SignIn,
            EntityType,
            IdentityAudit.EntityId(userId),
            before: null,
            after: new
            {
                passwordChangeRequired = refreshToken is null,
                clientIp = context.ClientIp,
                userAgent = context.UserAgent
            },
            context.CorrelationId,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public Task<StoredRefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        dbContext.Set<RefreshTokenEntity>()
            .AsNoTracking()
            .Where(token => token.TokenHash == tokenHash)
            .Select(token => new StoredRefreshToken(token.Id, token.UserId, token.ExpiresAt, token.RevokedAt))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<bool> RotateRefreshTokenAsync(
        long currentTokenId,
        NewRefreshToken replacement,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(context);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // The guard is the whole point: two requests replaying the same token race on this UPDATE and only
        // one can see revoked_at IS NULL, so only one successor is ever issued.
        var rows = await dbContext.Set<RefreshTokenEntity>()
            .Where(token => token.Id == currentTokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedReason, RefreshTokenRevocationReasons.Rotated),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var successor = ToEntity(replacement);
        dbContext.Set<RefreshTokenEntity>().Add(successor);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Set<RefreshTokenEntity>()
            .Where(token => token.Id == currentTokenId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(token => token.ReplacedByTokenId, successor.Id),
                cancellationToken);

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            replacement.UserId,
            AuthenticationAuditActions.Refresh,
            EntityType,
            IdentityAudit.EntityId(replacement.UserId),
            new { refreshTokenId = currentTokenId },
            new { refreshTokenId = successor.Id, clientIp = context.ClientIp, userAgent = context.UserAgent },
            context.CorrelationId,
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RevokeRefreshTokenAsync(
        long tokenId,
        string reason,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var userId = await dbContext.Set<RefreshTokenEntity>()
            .AsNoTracking()
            .Where(token => token.Id == tokenId)
            .Select(token => (long?)token.UserId)
            .SingleOrDefaultAsync(cancellationToken);

        var rows = await dbContext.Set<RefreshTokenEntity>()
            .Where(token => token.Id == tokenId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedReason, reason),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<AuditLogEntity>().Add(RevocationAudit(userId, reason, 1, tokenId, context, now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllRefreshTokensAsync(
        long userId,
        string reason,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await RevokeActiveTokensAsync(userId, reason, now, cancellationToken);
        dbContext.Set<AuditLogEntity>().Add(RevocationAudit(userId, reason, rows, tokenId: null, context, now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return rows;
    }

    public async Task<bool> ChangePasswordAsync(PasswordChange change, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(change);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Set<UserCredentialEntity>()
            .Where(entity => entity.UserId == change.UserId && entity.Version == change.ExpectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(entity => entity.PasswordHash, change.PasswordHash)
                    .SetProperty(entity => entity.PasswordAlgorithm, change.Algorithm)
                    .SetProperty(entity => entity.MustChangePassword, false)
                    .SetProperty(entity => entity.PasswordUpdatedAt, change.OccurredAt)
                    .SetProperty(entity => entity.FailedAttempts, 0)
                    .SetProperty(entity => entity.LockedUntil, (DateTimeOffset?)null)
                    .SetProperty(entity => entity.UpdatedAt, change.OccurredAt)
                    .SetProperty(entity => entity.Version, entity => entity.Version + 1),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Changing the secret ends every other session: a password change is the standard response to a
        // suspected compromise, and leaving old refresh tokens alive would defeat it.
        var revoked = await RevokeActiveTokensAsync(
            change.UserId, RefreshTokenRevocationReasons.PasswordChanged, change.OccurredAt, cancellationToken);

        dbContext.Set<AuditLogEntity>().Add(IdentityAudit.Entry(
            change.UserId,
            AuthenticationAuditActions.PasswordChange,
            EntityType,
            IdentityAudit.EntityId(change.UserId),
            new { version = change.ExpectedVersion },
            new { algorithm = change.Algorithm, revokedRefreshTokens = revoked, clientIp = change.Context.ClientIp },
            change.Context.CorrelationId,
            change.OccurredAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private Task<int> RevokeActiveTokensAsync(long userId, string reason, DateTimeOffset now, CancellationToken cancellationToken) =>
        dbContext.Set<RefreshTokenEntity>()
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.RevokedReason, reason),
                cancellationToken);

    private static AuditLogEntity RevocationAudit(
        long? userId,
        string reason,
        int revokedCount,
        long? tokenId,
        SignInContext context,
        DateTimeOffset now)
    {
        var (action, result) = reason switch
        {
            RefreshTokenRevocationReasons.Logout => (AuthenticationAuditActions.SignOut, IdentityAudit.Succeeded),
            RefreshTokenRevocationReasons.ReuseDetected => (AuthenticationAuditActions.TokenReuseDetected, IdentityAudit.Rejected),
            RefreshTokenRevocationReasons.RevokedByAdmin => (UserAccountAuditActions.PasswordReset, IdentityAudit.Succeeded),
            _ => (AuthenticationAuditActions.Refresh, IdentityAudit.Rejected)
        };

        return IdentityAudit.Entry(
            userId,
            action,
            RefreshTokenEntityType,
            IdentityAudit.EntityId(userId),
            before: null,
            after: new { reason, revokedRefreshTokens = revokedCount, refreshTokenId = tokenId, clientIp = context.ClientIp },
            context.CorrelationId,
            now,
            result);
    }

    private static RefreshTokenEntity ToEntity(NewRefreshToken token) => new()
    {
        UserId = token.UserId,
        TokenHash = token.TokenHash,
        IssuedAt = token.IssuedAt,
        ExpiresAt = token.ExpiresAt,
        ClientIp = token.ClientIp,
        UserAgent = token.UserAgent
    };

    private async Task<UserSignInRecord> BuildAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var credential = await dbContext.Set<UserCredentialEntity>()
            .AsNoTracking()
            .Where(entity => entity.UserId == user.Id)
            .Select(entity => new StoredCredential(
                entity.PasswordHash,
                entity.PasswordAlgorithm,
                entity.MustChangePassword,
                entity.FailedAttempts,
                entity.LockedUntil,
                entity.Version))
            .SingleOrDefaultAsync(cancellationToken);

        var employeeId = await dbContext.Set<EmployeeEntity>()
            .AsNoTracking()
            .Where(employee => employee.UserId == user.Id)
            .Select(employee => (long?)employee.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return new UserSignInRecord(user.Id, user.Email, user.DisplayName, user.Status, employeeId, credential);
    }
}
