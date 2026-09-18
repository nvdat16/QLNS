using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// Persistence contract of sign-in (ADM-01). Every write pairs its business change with the audit row in
/// one transaction — a sign-in that is not auditable must not succeed (quality goal Q1, architecture §6.5).
/// The repository decides nothing: it is told which counters to store and which reason to revoke with.
/// </summary>
public interface IAuthenticationRepository
{
    /// <summary>Lookup by the normalized (lower-case, trimmed) e-mail; null when no such account exists.</summary>
    Task<UserSignInRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<UserSignInRecord?> FindByUserIdAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Effective grants and permissions of the user (user_roles ⋈ role_permissions). Returns
    /// <see cref="IdentityAuthorization.None"/> for a user without grants.
    /// </summary>
    Task<IdentityAuthorization> GetAuthorizationAsync(long userId, CancellationToken cancellationToken);

    /// <summary>
    /// Persists the audit row of a rejected attempt and, when <see cref="SignInFailure.Credential"/> is set,
    /// the new failed-attempt counter and lockout instant.
    /// </summary>
    Task RecordFailedSignInAsync(SignInFailure failure, CancellationToken cancellationToken);

    /// <summary>
    /// Clears the lockout counters, stamps last_login_at, inserts <paramref name="refreshToken"/> when the
    /// session is a full one, and writes the <c>admin.auth.sign_in</c> audit row.
    /// </summary>
    Task RecordSuccessfulSignInAsync(
        long userId,
        NewRefreshToken? refreshToken,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<StoredRefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Consumes <paramref name="currentTokenId"/> and inserts its successor in one transaction. The update is
    /// guarded by <c>revoked_at IS NULL</c>, so two concurrent refreshes with the same token cannot both win;
    /// false means the token was already consumed.
    /// </summary>
    Task<bool> RotateRefreshTokenAsync(
        long currentTokenId,
        NewRefreshToken replacement,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Revokes one token; false when it was already revoked.</summary>
    Task<bool> RevokeRefreshTokenAsync(
        long tokenId,
        string reason,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Revokes every active token of the user — used on logout-all, password change and reuse detection.</summary>
    Task<int> RevokeAllRefreshTokensAsync(
        long userId,
        string reason,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>
    /// Stores the new hash, clears must_change_password and the lockout counters, revokes every refresh token
    /// of the user and audits the change. False when user_credentials.version no longer matches.
    /// </summary>
    Task<bool> ChangePasswordAsync(PasswordChange change, CancellationToken cancellationToken);
}
