using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Identity.IdentityTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Identity.Authentication;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task SignInAsync_ValidPassword_IssuesSessionWithResolvedAuthorization()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, issuer) = CreateService(repository);

        var session = await service.SignInAsync(new SignInCommand(Email, Password, Context()), CancellationToken.None);

        Assert.Equal("access-1", session.AccessToken.Value);
        Assert.Equal("refresh-1", session.RefreshToken);
        Assert.False(session.Identity.PasswordChangeRequired);
        Assert.Equal(DataScopeTypeNames.Department, session.Identity.DataScopeClaim);
        Assert.Equal([2L, 4L], session.Identity.DataScope.DepartmentIds.Order());
        Assert.Contains("corehr.probation.read", session.Identity.Permissions);
        Assert.Equal(1, repository.SuccessfulSignIns);
        Assert.Empty(repository.Failures);
        Assert.Equal(1, issuer.IssueCalls);
    }

    [Fact]
    public async Task SignInAsync_RefreshTokenStoredAsFingerprintWithConfiguredLifetime()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository);

        var session = await service.SignInAsync(new SignInCommand(Email, Password, Context()), CancellationToken.None);

        var stored = Assert.Single(repository.IssuedTokens);
        Assert.Equal("fp(refresh-1)", stored.TokenHash);
        Assert.NotEqual(session.RefreshToken, stored.TokenHash);
        Assert.Equal(Now.AddDays(14), stored.ExpiresAt);
        Assert.Equal("10.0.0.7", stored.ClientIp);
    }

    [Fact]
    public async Task SignInAsync_UnknownEmail_VerifiesDummyHashAndAuditsWithoutCounters()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository, out var hasher);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.SignInAsync(
            new SignInCommand("nobody@qlns.local", Password, Context()), CancellationToken.None));

        Assert.Equal(1, hasher.DummyVerifyCalls);
        var failure = Assert.Single(repository.Failures);
        Assert.Equal(AuthenticationFailedException.InvalidCredentialsCode, failure.Code);
        Assert.Null(failure.UserId);
        Assert.Null(failure.Credential);
        Assert.Equal(0, repository.SuccessfulSignIns);
    }

    [Fact]
    public async Task SignInAsync_WrongPassword_RecordsIncrementedAttemptCounter()
    {
        var repository = new FakeAuthenticationRepository(Record(Credential(failedAttempts: 1)));
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.SignInAsync(
            new SignInCommand(Email, "wrong-password", Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.InvalidCredentialsCode, exception.Code);
        var failure = Assert.Single(repository.Failures);
        Assert.Equal(2, failure.Credential!.FailedAttempts);
        Assert.Null(failure.Credential.LockedUntil);
    }

    [Fact]
    public async Task SignInAsync_FailureReachingThreshold_StartsLockoutWindow()
    {
        var repository = new FakeAuthenticationRepository(
            Record(Credential(failedAttempts: SignInLockout.MaxFailedAttempts - 1)));
        var (service, _) = CreateService(repository);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.SignInAsync(
            new SignInCommand(Email, "wrong-password", Context()), CancellationToken.None));

        var failure = Assert.Single(repository.Failures);
        Assert.Equal(SignInLockout.MaxFailedAttempts, failure.Credential!.FailedAttempts);
        Assert.Equal(Now + SignInLockout.Window, failure.Credential.LockedUntil);
    }

    [Fact]
    public async Task SignInAsync_LockedAccount_RejectsWithRetryAfterAndWithoutVerifying()
    {
        var repository = new FakeAuthenticationRepository(
            Record(Credential(failedAttempts: 5, lockedUntil: Now.AddMinutes(4))));
        var (service, _) = CreateService(repository, out var hasher);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.SignInAsync(
            new SignInCommand(Email, Password, Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.AccountLockedCode, exception.Code);
        Assert.Equal(240, Assert.Contains("retryAfterSeconds", exception.Details));
        Assert.Equal(0, hasher.VerifyCalls);
        Assert.Equal(0, repository.SuccessfulSignIns);
    }

    [Fact]
    public async Task SignInAsync_ExpiredLockout_SignsInAgain()
    {
        var repository = new FakeAuthenticationRepository(
            Record(Credential(failedAttempts: 5, lockedUntil: Now.AddSeconds(-1))));
        var (service, _) = CreateService(repository);

        var session = await service.SignInAsync(new SignInCommand(Email, Password, Context()), CancellationToken.None);

        Assert.NotNull(session.RefreshToken);
        Assert.Equal(1, repository.SuccessfulSignIns);
    }

    [Fact]
    public async Task SignInAsync_DisabledAccount_IsRejectedOnlyAfterPasswordVerification()
    {
        var repository = new FakeAuthenticationRepository(Record(status: UserSignInRecord.DisabledStatus));
        var (service, _) = CreateService(repository, out var hasher);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.SignInAsync(
            new SignInCommand(Email, Password, Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.AccountDisabledCode, exception.Code);
        Assert.Equal(1, hasher.VerifyCalls);
        Assert.Equal(AuthenticationFailedException.AccountDisabledCode, Assert.Single(repository.Failures).Code);
    }

    [Fact]
    public async Task SignInAsync_DisabledAccountWithWrongPassword_LooksLikeAnyOtherBadPassword()
    {
        var repository = new FakeAuthenticationRepository(Record(status: UserSignInRecord.DisabledStatus));
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.SignInAsync(
            new SignInCommand(Email, "wrong-password", Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.InvalidCredentialsCode, exception.Code);
    }

    [Fact]
    public async Task SignInAsync_PasswordChangePending_IssuesRestrictedSessionWithoutRefreshToken()
    {
        var repository = new FakeAuthenticationRepository(Record(Credential(mustChangePassword: true)));
        var (service, issuer) = CreateService(repository);

        var session = await service.SignInAsync(new SignInCommand(Email, Password, Context()), CancellationToken.None);

        Assert.True(session.Identity.PasswordChangeRequired);
        Assert.Null(session.RefreshToken);
        Assert.Empty(session.Identity.Permissions);
        Assert.Empty(session.Identity.Roles);
        Assert.Equal(DataScopeTypeNames.Self, session.Identity.DataScopeClaim);
        Assert.True(issuer.LastIdentity!.PasswordChangeRequired);
        Assert.Equal(0, repository.AuthorizationReads);
        Assert.Empty(repository.IssuedTokens);
    }

    [Fact]
    public async Task RefreshAsync_ActiveToken_RotatesAndRereadsAuthorization()
    {
        var repository = new FakeAuthenticationRepository(Record())
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: null)
        };
        var (service, _) = CreateService(repository);

        var session = await service.RefreshAsync(new RefreshSessionCommand("presented", Context()), CancellationToken.None);

        Assert.Equal(41, repository.LastRotatedTokenId);
        Assert.Equal("refresh-1", session.RefreshToken);
        Assert.Equal(Now.AddDays(14), session.RefreshTokenExpiresAt);
        Assert.Equal(1, repository.AuthorizationReads);
        Assert.Empty(repository.RevokedFamilies);
    }

    [Fact]
    public async Task RefreshAsync_RevokedToken_RevokesWholeFamilyAsReuse()
    {
        var repository = new FakeAuthenticationRepository(Record())
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: Now.AddMinutes(-5))
        };
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.RefreshAsync(
            new RefreshSessionCommand("presented", Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.InvalidRefreshTokenCode, exception.Code);
        Assert.Equal((1L, RefreshTokenRevocationReasons.ReuseDetected), Assert.Single(repository.RevokedFamilies));
    }

    [Fact]
    public async Task RefreshAsync_ExpiredToken_IsRejectedWithoutRevokingTheFamily()
    {
        var repository = new FakeAuthenticationRepository(Record())
        {
            StoredToken = new StoredRefreshToken(41, 1, Now, RevokedAt: null)
        };
        var (service, _) = CreateService(repository);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.RefreshAsync(
            new RefreshSessionCommand("presented", Context()), CancellationToken.None));

        Assert.Empty(repository.RevokedFamilies);
        Assert.Null(repository.LastRotatedTokenId);
    }

    [Fact]
    public async Task RefreshAsync_UnknownToken_IsRejected()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.RefreshAsync(
            new RefreshSessionCommand("presented", Context()), CancellationToken.None));
    }

    [Fact]
    public async Task RefreshAsync_LostRotationRace_IsRejected()
    {
        var repository = new FakeAuthenticationRepository(Record())
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: null),
            RotateSucceeds = false
        };
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.RefreshAsync(
            new RefreshSessionCommand("presented", Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.InvalidRefreshTokenCode, exception.Code);
    }

    [Fact]
    public async Task RefreshAsync_AccountDisabledSinceSignIn_EndsEverySession()
    {
        var repository = new FakeAuthenticationRepository(Record(status: UserSignInRecord.DisabledStatus))
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: null)
        };
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.RefreshAsync(
            new RefreshSessionCommand("presented", Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.AccountDisabledCode, exception.Code);
        Assert.Equal((1L, RefreshTokenRevocationReasons.AccountDisabled), Assert.Single(repository.RevokedFamilies));
    }

    [Fact]
    public async Task RefreshAsync_PasswordChangePending_CannotRenewTheSession()
    {
        var repository = new FakeAuthenticationRepository(Record(Credential(mustChangePassword: true)))
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: null)
        };
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.RefreshAsync(
            new RefreshSessionCommand("presented", Context()), CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.PasswordChangeRequiredCode, exception.Code);
    }

    [Fact]
    public async Task SignOutAsync_ActiveToken_RevokesItWithLogoutReason()
    {
        var repository = new FakeAuthenticationRepository(Record())
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: null)
        };
        var (service, _) = CreateService(repository);

        await service.SignOutAsync(new SignOutCommand("presented", Context()), CancellationToken.None);

        Assert.Equal((41L, RefreshTokenRevocationReasons.Logout), Assert.Single(repository.RevokedTokens));
    }

    [Fact]
    public async Task SignOutAsync_UnknownOrAlreadyRevokedToken_SucceedsSilently()
    {
        var repository = new FakeAuthenticationRepository(Record())
        {
            StoredToken = new StoredRefreshToken(41, 1, Now.AddDays(3), RevokedAt: Now.AddMinutes(-1))
        };
        var (service, _) = CreateService(repository);

        await service.SignOutAsync(new SignOutCommand("presented", Context()), CancellationToken.None);
        await service.SignOutAsync(new SignOutCommand(null, Context()), CancellationToken.None);

        Assert.Empty(repository.RevokedTokens);
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_ValidCurrentPassword_StoresNewHashGuardedByVersion()
    {
        var repository = new FakeAuthenticationRepository(Record(Credential(version: 7)));
        var (service, _) = CreateService(repository);

        await service.ChangeOwnPasswordAsync(
            new ChangeOwnPasswordCommand(Password, "Brand-New-Secret-1", Actor(), Context()), CancellationToken.None);

        var change = Assert.Single(repository.PasswordChanges);
        Assert.Equal(7, change.ExpectedVersion);
        Assert.Equal(FakePasswordHasher.HashOf("Brand-New-Secret-1"), change.PasswordHash);
        Assert.Equal(FakePasswordHasher.AlgorithmName, change.Algorithm);
        Assert.Empty(repository.Failures);
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WrongCurrentPassword_CountsAsFailedAttempt()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<AuthenticationFailedException>(() => service.ChangeOwnPasswordAsync(
            new ChangeOwnPasswordCommand("wrong-password", "Brand-New-Secret-1", Actor(), Context()),
            CancellationToken.None));

        Assert.Equal(AuthenticationFailedException.InvalidCredentialsCode, exception.Code);
        Assert.Equal(1, Assert.Single(repository.Failures).Credential!.FailedAttempts);
        Assert.Empty(repository.PasswordChanges);
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_NewPasswordEqualsCurrent_IsRejected()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.ChangeOwnPasswordAsync(
            new ChangeOwnPasswordCommand(Password, Password, Actor(), Context()), CancellationToken.None));

        Assert.Contains("newPassword", exception.Errors);
        Assert.Empty(repository.PasswordChanges);
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_WeakNewPassword_IsRejectedBeforeAnyWrite()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.ChangeOwnPasswordAsync(
            new ChangeOwnPasswordCommand(Password, "short", Actor(), Context()), CancellationToken.None));

        Assert.Empty(repository.PasswordChanges);
    }

    [Fact]
    public async Task ChangeOwnPasswordAsync_StaleCredentialVersion_ThrowsConcurrency()
    {
        var repository = new FakeAuthenticationRepository(Record()) { ChangePasswordSucceeds = false };
        var (service, _) = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ChangeOwnPasswordAsync(
            new ChangeOwnPasswordCommand(Password, "Brand-New-Secret-1", Actor(), Context()), CancellationToken.None));
    }

    [Fact]
    public async Task DescribeAsync_PasswordChangePending_ReportsNoPermissions()
    {
        var repository = new FakeAuthenticationRepository(Record(Credential(mustChangePassword: true)));
        var (service, _) = CreateService(repository);

        var identity = await service.DescribeAsync(Actor(), CancellationToken.None);

        Assert.True(identity.PasswordChangeRequired);
        Assert.Empty(identity.Permissions);
    }

    [Fact]
    public async Task DescribeAsync_UnmappedActor_ThrowsNotFound()
    {
        var repository = new FakeAuthenticationRepository(Record());
        var (service, _) = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.DescribeAsync(
            Actor(userId: 99), CancellationToken.None));
    }

    private static (AuthenticationService Service, FakeAccessTokenIssuer Issuer) CreateService(
        FakeAuthenticationRepository repository) =>
        CreateService(repository, out _);

    private static (AuthenticationService Service, FakeAccessTokenIssuer Issuer) CreateService(
        FakeAuthenticationRepository repository,
        out FakePasswordHasher hasher)
    {
        hasher = new FakePasswordHasher();
        var issuer = new FakeAccessTokenIssuer();
        var service = new AuthenticationService(
            repository,
            hasher,
            issuer,
            new FakeRefreshTokenGenerator(),
            new AuthenticationSettings(),
            new FixedTimeProvider(Now));

        return (service, issuer);
    }
}
