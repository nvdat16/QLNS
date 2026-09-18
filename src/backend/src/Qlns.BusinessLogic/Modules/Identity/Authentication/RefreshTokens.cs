namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>Stored state of a refresh token; the token itself is never persisted, only its fingerprint.</summary>
public sealed record StoredRefreshToken(
    long Id,
    long UserId,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt)
{
    public bool IsRevoked => RevokedAt is not null;

    public bool IsExpired(DateTimeOffset now) => ExpiresAt <= now;

    public bool IsUsable(DateTimeOffset now) => !IsRevoked && !IsExpired(now);
}

/// <summary>A refresh token about to be inserted into refresh_tokens.</summary>
public sealed record NewRefreshToken(
    long UserId,
    string TokenHash,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? ClientIp,
    string? UserAgent);

/// <summary>refresh_tokens.revoked_reason — constrained by <c>ck_refresh_tokens_reason</c>.</summary>
public static class RefreshTokenRevocationReasons
{
    /// <summary>Single-use token consumed by a successful refresh.</summary>
    public const string Rotated = "rotated";

    public const string Logout = "logout";

    public const string PasswordChanged = "password_changed";

    /// <summary>An already revoked token was presented again: the whole family is dropped.</summary>
    public const string ReuseDetected = "reuse_detected";

    public const string RevokedByAdmin = "revoked_by_admin";

    public const string AccountDisabled = "account_disabled";
}
