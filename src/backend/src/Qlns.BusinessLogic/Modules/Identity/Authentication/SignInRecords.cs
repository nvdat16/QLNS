namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// The account as needed for sign-in: identity columns of <c>users</c>, the linked employee id
/// (employees.user_id) and the local credential when one exists.
/// </summary>
public sealed record UserSignInRecord(
    long UserId,
    string Email,
    string DisplayName,
    string Status,
    long? EmployeeId,
    StoredCredential? Credential)
{
    public const string ActiveStatus = "active";
    public const string DisabledStatus = "disabled";

    public bool IsActive => string.Equals(Status, ActiveStatus, StringComparison.Ordinal);
}

/// <summary>One row of user_credentials. <see cref="PasswordHash"/> encodes its own algorithm parameters.</summary>
public sealed record StoredCredential(
    string PasswordHash,
    string Algorithm,
    bool MustChangePassword,
    int FailedAttempts,
    DateTimeOffset? LockedUntil,
    long Version);

/// <summary>
/// Progressive lockout after consecutive failures. Counted per credential, cleared on the first success;
/// the window is short enough to stay usable and long enough to make online guessing pointless.
/// </summary>
public static class SignInLockout
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public static bool IsLocked(StoredCredential credential, DateTimeOffset now) =>
        credential.LockedUntil is { } until && until > now;

    public static TimeSpan RetryAfter(StoredCredential credential, DateTimeOffset now) =>
        credential.LockedUntil is { } until && until > now ? until - now : TimeSpan.Zero;

    /// <summary>
    /// Counter state after one more failure. The attempt that reaches <see cref="MaxFailedAttempts"/>
    /// starts the lockout window; a failure while already locked does not extend it.
    /// </summary>
    public static CredentialFailureUpdate NextFailure(StoredCredential credential, DateTimeOffset now)
    {
        var attempts = credential.FailedAttempts + 1;
        if (attempts < MaxFailedAttempts)
        {
            return new CredentialFailureUpdate(attempts, credential.LockedUntil);
        }

        return new CredentialFailureUpdate(attempts, now + Window);
    }
}

/// <summary>New values of the two lockout columns of user_credentials.</summary>
public sealed record CredentialFailureUpdate(int FailedAttempts, DateTimeOffset? LockedUntil);
