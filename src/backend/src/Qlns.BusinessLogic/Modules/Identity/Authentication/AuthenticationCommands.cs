using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// Transport-level context of an authentication call. Recorded on the refresh token and in the audit
/// trail so a stolen session can be traced; never used to make an authorization decision.
/// </summary>
public sealed record SignInContext(string? ClientIp, string? UserAgent, string CorrelationId)
{
    public const int ClientIpMaxLength = 45;
    public const int UserAgentMaxLength = 255;

    /// <summary>Truncates both fields to their column widths; longer values are attacker-controlled.</summary>
    public SignInContext Normalized() => new(
        Truncate(ClientIp, ClientIpMaxLength),
        Truncate(UserAgent, UserAgentMaxLength),
        CorrelationId);

    private static string? Truncate(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= maxLength ? value
        : value[..maxLength];
}

public sealed record SignInCommand(string? Email, string? Password, SignInContext Context);

public sealed record RefreshSessionCommand(string? RefreshToken, SignInContext Context);

public sealed record SignOutCommand(string? RefreshToken, SignInContext Context);

public sealed record ChangeOwnPasswordCommand(
    string? CurrentPassword,
    string? NewPassword,
    CoreHrActor Actor,
    SignInContext Context);

/// <summary>A failed sign-in or refresh, with the credential counters to persist when one applies.</summary>
public sealed record SignInFailure(
    long? UserId,
    string Email,
    string Code,
    CredentialFailureUpdate? Credential,
    SignInContext Context,
    DateTimeOffset OccurredAt);

/// <summary>A verified password change: new hash plus the guard on user_credentials.version.</summary>
public sealed record PasswordChange(
    long UserId,
    string PasswordHash,
    string Algorithm,
    long ExpectedVersion,
    SignInContext Context,
    DateTimeOffset OccurredAt);

/// <summary>Stable audit_logs.action values of the module (ADM-01).</summary>
public static class AuthenticationAuditActions
{
    public const string SignIn = "admin.auth.sign_in";
    public const string Refresh = "admin.auth.refresh";
    public const string SignOut = "admin.auth.sign_out";
    public const string PasswordChange = "admin.auth.password_change";
    public const string TokenReuseDetected = "admin.auth.token_reuse_detected";
    public const string EntityType = "user";
}
