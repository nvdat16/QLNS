using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.UnitTests.Modules.Identity.Authentication;

/// <summary>
/// In-memory <see cref="IAuthenticationRepository"/>. It records what the service asked it to persist rather
/// than simulating SQL, so the tests assert on decisions (which counters, which revocation reason, which
/// guard version) instead of on storage behaviour.
/// </summary>
internal sealed class FakeAuthenticationRepository(UserSignInRecord? record = null) : IAuthenticationRepository
{
    public UserSignInRecord? Record { get; set; } = record;
    public IdentityAuthorization Authorization { get; set; } = IdentityTestData.Authorization();
    public StoredRefreshToken? StoredToken { get; set; }
    public bool RotateSucceeds { get; set; } = true;
    public bool ChangePasswordSucceeds { get; set; } = true;

    public List<SignInFailure> Failures { get; } = [];
    public List<NewRefreshToken> IssuedTokens { get; } = [];
    public List<(long UserId, string Reason)> RevokedFamilies { get; } = [];
    public List<(long TokenId, string Reason)> RevokedTokens { get; } = [];
    public List<PasswordChange> PasswordChanges { get; } = [];

    public int SuccessfulSignIns { get; private set; }
    public int AuthorizationReads { get; private set; }
    public long? LastRotatedTokenId { get; private set; }
    public NewRefreshToken? LastSignInToken { get; private set; }

    public Task<UserSignInRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        Task.FromResult(Record is not null && Record.Email == normalizedEmail ? Record : null);

    public Task<UserSignInRecord?> FindByUserIdAsync(long userId, CancellationToken cancellationToken) =>
        Task.FromResult(Record is not null && Record.UserId == userId ? Record : null);

    public Task<IdentityAuthorization> GetAuthorizationAsync(long userId, CancellationToken cancellationToken)
    {
        AuthorizationReads++;
        return Task.FromResult(Authorization);
    }

    public Task RecordFailedSignInAsync(SignInFailure failure, CancellationToken cancellationToken)
    {
        Failures.Add(failure);
        return Task.CompletedTask;
    }

    public Task RecordSuccessfulSignInAsync(
        long userId,
        NewRefreshToken? refreshToken,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        SuccessfulSignIns++;
        LastSignInToken = refreshToken;
        if (refreshToken is not null)
        {
            IssuedTokens.Add(refreshToken);
        }

        return Task.CompletedTask;
    }

    public Task<StoredRefreshToken?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(StoredToken);

    public Task<bool> RotateRefreshTokenAsync(
        long currentTokenId,
        NewRefreshToken replacement,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        LastRotatedTokenId = currentTokenId;
        if (RotateSucceeds)
        {
            IssuedTokens.Add(replacement);
        }

        return Task.FromResult(RotateSucceeds);
    }

    public Task<bool> RevokeRefreshTokenAsync(
        long tokenId,
        string reason,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RevokedTokens.Add((tokenId, reason));
        return Task.FromResult(true);
    }

    public Task<int> RevokeAllRefreshTokensAsync(
        long userId,
        string reason,
        SignInContext context,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RevokedFamilies.Add((userId, reason));
        return Task.FromResult(1);
    }

    public Task<bool> ChangePasswordAsync(PasswordChange change, CancellationToken cancellationToken)
    {
        PasswordChanges.Add(change);
        return Task.FromResult(ChangePasswordSucceeds);
    }
}
