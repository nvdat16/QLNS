using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// ADM-01: password sign-in, refresh-token rotation, sign-out and self-service password change.
/// <para>
/// Three rules drive the implementation. (1) Nothing about an account leaks through a failure: an unknown
/// e-mail is verified against <see cref="IPasswordHasher.DummyHash"/> so it costs the same as a real one,
/// and every rejection returns the same 401 shape. (2) Permissions and data scope are read from the
/// database on every sign-in and on every refresh, never from the client, so a revoked role takes effect
/// within one access-token lifetime. (3) Refresh tokens are single-use; replaying one is treated as theft
/// and drops the whole family.
/// </para>
/// </summary>
public sealed class AuthenticationService(
    IAuthenticationRepository repository,
    IPasswordHasher passwordHasher,
    IAccessTokenIssuer accessTokenIssuer,
    IRefreshTokenGenerator refreshTokens,
    AuthenticationSettings settings,
    TimeProvider timeProvider)
{
    public async Task<AuthenticationSession> SignInAsync(SignInCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = command.Context.Normalized();
        var now = timeProvider.GetUtcNow();
        var email = EmailAddress.Normalize(command.Email);
        var record = email.Length == 0 ? null : await repository.FindByEmailAsync(email, cancellationToken);

        // Unknown account: still run one verification so that response time does not disclose existence.
        if (record?.Credential is not { } credential)
        {
            passwordHasher.Verify(passwordHasher.DummyHash, command.Password ?? string.Empty);
            await FailAsync(
                new SignInFailure(record?.UserId, email, AuthenticationFailedException.InvalidCredentialsCode, null, context, now),
                cancellationToken);
            throw AuthenticationFailedException.InvalidCredentials();
        }

        if (SignInLockout.IsLocked(credential, now))
        {
            var retryAfter = SignInLockout.RetryAfter(credential, now);
            await FailAsync(
                new SignInFailure(record.UserId, email, AuthenticationFailedException.AccountLockedCode, null, context, now),
                cancellationToken);
            throw new AuthenticationFailedException(
                AuthenticationFailedException.AccountLockedCode,
                $"The account is temporarily locked after {SignInLockout.MaxFailedAttempts} failed attempts. Try again later.")
            {
                Details = new Dictionary<string, object?> { ["retryAfterSeconds"] = (int)Math.Ceiling(retryAfter.TotalSeconds) }
            };
        }

        var verification = passwordHasher.Verify(credential.PasswordHash, command.Password ?? string.Empty);
        if (verification == PasswordVerification.Failed)
        {
            await FailAsync(
                new SignInFailure(
                    record.UserId,
                    email,
                    AuthenticationFailedException.InvalidCredentialsCode,
                    SignInLockout.NextFailure(credential, now),
                    context,
                    now),
                cancellationToken);
            throw AuthenticationFailedException.InvalidCredentials();
        }

        // A disabled account is rejected only after the password was verified, so that the distinction
        // between "wrong password" and "disabled" is never visible to someone who does not know the secret.
        if (!record.IsActive)
        {
            await FailAsync(
                new SignInFailure(record.UserId, email, AuthenticationFailedException.AccountDisabledCode, null, context, now),
                cancellationToken);
            throw new AuthenticationFailedException(
                AuthenticationFailedException.AccountDisabledCode,
                "The account is disabled. Contact a system administrator.");
        }

        if (credential.MustChangePassword)
        {
            // Restricted session: no permissions, no refresh token — only the change-password endpoint.
            var pending = AuthenticatedIdentity.PendingPasswordChange(
                record.UserId, record.EmployeeId, record.Email, record.DisplayName);
            await repository.RecordSuccessfulSignInAsync(record.UserId, refreshToken: null, context, now, cancellationToken);
            return new AuthenticationSession(pending, accessTokenIssuer.Issue(pending, now), RefreshToken: null, RefreshTokenExpiresAt: null);
        }

        var authorization = await repository.GetAuthorizationAsync(record.UserId, cancellationToken);
        var identity = AuthenticatedIdentity.From(record, authorization);
        var (token, refreshToken) = NewRefreshToken(record.UserId, context, now);

        await repository.RecordSuccessfulSignInAsync(record.UserId, refreshToken, context, now, cancellationToken);

        return new AuthenticationSession(
            identity,
            accessTokenIssuer.Issue(identity, now),
            token,
            refreshToken.ExpiresAt);
    }

    public async Task<AuthenticationSession> RefreshAsync(RefreshSessionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var context = command.Context.Normalized();
        var now = timeProvider.GetUtcNow();

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            throw AuthenticationFailedException.InvalidRefreshToken();
        }

        var stored = await repository.FindRefreshTokenAsync(refreshTokens.Fingerprint(command.RefreshToken), cancellationToken);
        if (stored is null || stored.IsExpired(now))
        {
            throw AuthenticationFailedException.InvalidRefreshToken();
        }

        if (stored.IsRevoked)
        {
            // Replay of a consumed token: assume the token family is compromised and end every session.
            await repository.RevokeAllRefreshTokensAsync(
                stored.UserId, RefreshTokenRevocationReasons.ReuseDetected, context, now, cancellationToken);
            throw AuthenticationFailedException.InvalidRefreshToken();
        }

        var record = await repository.FindByUserIdAsync(stored.UserId, cancellationToken);
        if (record is null || !record.IsActive)
        {
            await repository.RevokeAllRefreshTokensAsync(
                stored.UserId, RefreshTokenRevocationReasons.AccountDisabled, context, now, cancellationToken);
            throw new AuthenticationFailedException(
                AuthenticationFailedException.AccountDisabledCode,
                "The account is disabled. Contact a system administrator.");
        }

        if (record.Credential?.MustChangePassword != false)
        {
            await repository.RevokeAllRefreshTokensAsync(
                stored.UserId, RefreshTokenRevocationReasons.PasswordChanged, context, now, cancellationToken);
            throw new AuthenticationFailedException(
                AuthenticationFailedException.PasswordChangeRequiredCode,
                "The password must be changed before the session can be renewed. Sign in again.");
        }

        var (token, replacement) = NewRefreshToken(stored.UserId, context, now);
        if (!await repository.RotateRefreshTokenAsync(stored.Id, replacement, context, now, cancellationToken))
        {
            throw AuthenticationFailedException.InvalidRefreshToken();
        }

        var authorization = await repository.GetAuthorizationAsync(record.UserId, cancellationToken);
        var identity = AuthenticatedIdentity.From(record, authorization);

        return new AuthenticationSession(
            identity,
            accessTokenIssuer.Issue(identity, now),
            token,
            replacement.ExpiresAt);
    }

    /// <summary>
    /// Revokes the presented refresh token. Always succeeds from the caller's point of view: an unknown or
    /// already revoked token is not reported, because sign-out must not become an oracle for token validity.
    /// </summary>
    public async Task SignOutAsync(SignOutCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return;
        }

        var context = command.Context.Normalized();
        var now = timeProvider.GetUtcNow();
        var stored = await repository.FindRefreshTokenAsync(refreshTokens.Fingerprint(command.RefreshToken), cancellationToken);
        if (stored is null || !stored.IsUsable(now))
        {
            return;
        }

        await repository.RevokeRefreshTokenAsync(
            stored.Id, RefreshTokenRevocationReasons.Logout, context, now, cancellationToken);
    }

    /// <summary>
    /// Self-service password change. Requires the current password even though the caller is authenticated,
    /// so that a stolen access token alone cannot take over the account. Succeeding invalidates every
    /// refresh token of the user, which forces all other devices to sign in again.
    /// </summary>
    public async Task ChangeOwnPasswordAsync(ChangeOwnPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(command.Actor);

        var context = command.Context.Normalized();
        var now = timeProvider.GetUtcNow();

        var record = await repository.FindByUserIdAsync(command.Actor.UserId, cancellationToken)
            ?? throw new CoreHrNotFoundException("User", command.Actor.UserId);

        var credential = record.Credential;
        if (credential is null ||
            passwordHasher.Verify(credential.PasswordHash, command.CurrentPassword ?? string.Empty) == PasswordVerification.Failed)
        {
            await FailAsync(
                new SignInFailure(
                    record.UserId,
                    record.Email,
                    AuthenticationFailedException.InvalidCredentialsCode,
                    credential is null ? null : SignInLockout.NextFailure(credential, now),
                    context,
                    now),
                cancellationToken);
            throw new AuthenticationFailedException(
                AuthenticationFailedException.InvalidCredentialsCode,
                "The current password is incorrect.");
        }

        PasswordPolicy.Validate(command.NewPassword, "newPassword", record.Email);

        if (passwordHasher.Verify(credential.PasswordHash, command.NewPassword!) != PasswordVerification.Failed)
        {
            throw CoreHrValidationException.For("newPassword", "The new password must differ from the current one.");
        }

        var change = new PasswordChange(
            record.UserId,
            passwordHasher.Hash(command.NewPassword!),
            passwordHasher.Algorithm,
            credential.Version,
            context,
            now);

        if (!await repository.ChangePasswordAsync(change, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException("user credential");
        }
    }

    /// <summary>Current identity of the caller, re-read from the database rather than from the token claims.</summary>
    public async Task<AuthenticatedIdentity> DescribeAsync(CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var record = await repository.FindByUserIdAsync(actor.UserId, cancellationToken)
            ?? throw new CoreHrNotFoundException("User", actor.UserId);

        if (record.Credential?.MustChangePassword == true)
        {
            return AuthenticatedIdentity.PendingPasswordChange(
                record.UserId, record.EmployeeId, record.Email, record.DisplayName);
        }

        var authorization = await repository.GetAuthorizationAsync(actor.UserId, cancellationToken);
        return AuthenticatedIdentity.From(record, authorization);
    }

    private Task FailAsync(SignInFailure failure, CancellationToken cancellationToken) =>
        repository.RecordFailedSignInAsync(failure, cancellationToken);

    private (string Token, NewRefreshToken Record) NewRefreshToken(long userId, SignInContext context, DateTimeOffset now)
    {
        var token = refreshTokens.NewToken();
        return (token, new NewRefreshToken(
            userId,
            refreshTokens.Fingerprint(token),
            now,
            now + settings.RefreshTokenLifetime,
            context.ClientIp,
            context.UserAgent));
    }
}
