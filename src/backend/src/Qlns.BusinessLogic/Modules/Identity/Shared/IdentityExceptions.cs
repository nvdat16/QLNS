namespace Qlns.BusinessLogic.Modules.Identity.Shared;

/// <summary>
/// Sign-in, refresh or password verification failed (HTTP 401). The message is safe to return to the
/// caller; it never reveals whether the e-mail exists. <see cref="Code"/> is the stable client contract.
/// </summary>
public sealed class AuthenticationFailedException(string code, string message) : Exception(message)
{
    /// <summary>E-mail unknown, no local credential, or wrong password.</summary>
    public const string InvalidCredentialsCode = "admin.auth.invalid_credentials";

    /// <summary>Too many consecutive failures; retry after the lockout window.</summary>
    public const string AccountLockedCode = "admin.auth.account_locked";

    /// <summary>The account exists but was disabled by an administrator.</summary>
    public const string AccountDisabledCode = "admin.auth.account_disabled";

    /// <summary>Refresh token unknown, expired, already rotated or replayed.</summary>
    public const string InvalidRefreshTokenCode = "admin.auth.invalid_refresh_token";

    /// <summary>The account must change its password before it can obtain a full session.</summary>
    public const string PasswordChangeRequiredCode = "admin.auth.password_change_required";

    public string Code { get; } = code;

    /// <summary>Optional structured detail, for example <c>retryAfterSeconds</c> for a locked account.</summary>
    public IReadOnlyDictionary<string, object?> Details { get; init; } = new Dictionary<string, object?>();

    public static AuthenticationFailedException InvalidCredentials() => new(
        InvalidCredentialsCode,
        "E-mail or password is incorrect.");

    public static AuthenticationFailedException InvalidRefreshToken() => new(
        InvalidRefreshTokenCode,
        "Refresh token is invalid, expired or has already been used. Sign in again.");
}
