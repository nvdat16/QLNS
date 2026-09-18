namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>A signed, self-contained bearer token and the instant it stops being accepted.</summary>
public sealed record IssuedAccessToken(string Value, string TokenId, DateTimeOffset ExpiresAt)
{
    public const string BearerTokenType = "Bearer";

    public int ExpiresInSeconds(DateTimeOffset now) => (int)Math.Max(0, (ExpiresAt - now).TotalSeconds);
}

/// <summary>
/// Port that turns an authenticated identity into an access token. The adapter decides the format and
/// signature; it must emit the claims <see cref="CoreHr.Shared.CoreHrActor"/> is resolved from
/// (<c>qlns_user_id</c>, <c>qlns_employee_id</c>, <c>data_scope</c>, <c>department_id</c>, <c>permission</c>),
/// otherwise every business endpoint would reject the token it issues.
/// </summary>
public interface IAccessTokenIssuer
{
    IssuedAccessToken Issue(AuthenticatedIdentity identity, DateTimeOffset now);
}

/// <summary>
/// Port for the opaque refresh token. Only <see cref="Fingerprint"/> of a token is persisted, so a dump of
/// refresh_tokens cannot be replayed against the API.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>A fresh token with at least 256 bits of entropy from a cryptographic RNG.</summary>
    string NewToken();

    /// <summary>Stable, collision-resistant hash of a token; 64 lower-case hex characters (refresh_tokens.token_hash).</summary>
    string Fingerprint(string token);
}
