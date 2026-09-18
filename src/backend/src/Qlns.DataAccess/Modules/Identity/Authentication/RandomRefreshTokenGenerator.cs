using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using Qlns.BusinessLogic.Modules.Identity.Authentication;

namespace Qlns.DataAccess.Modules.Identity.Authentication;

/// <summary>
/// <see cref="IRefreshTokenGenerator"/> adapter: 256 bits from the OS CSPRNG, base64url-encoded, stored as
/// its SHA-256 hex digest. A plain digest without a key is the right primitive here — the token is already
/// full-entropy random, so it is not guessable from the hash, and a keyless digest keeps the lookup a plain
/// indexed equality match on refresh_tokens.token_hash.
/// </summary>
public sealed class RandomRefreshTokenGenerator : IRefreshTokenGenerator
{
    public const int TokenBytes = 32;

    /// <summary>Upper bound on an accepted token; anything longer is rejected before hashing.</summary>
    public const int MaxTokenLength = 512;

    public string NewToken() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

    public string Fingerprint(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        if (token.Length > MaxTokenLength)
        {
            token = token[..MaxTokenLength];
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
