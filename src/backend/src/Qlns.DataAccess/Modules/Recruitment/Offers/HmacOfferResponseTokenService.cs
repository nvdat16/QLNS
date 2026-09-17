using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.DataAccess.Modules.Recruitment.Offers;

/// <summary>
/// <see cref="IOfferResponseTokenService"/> adapter: HMAC-SHA256 over <c>{offerId}|{expiresUnix}</c> with the key from
/// <see cref="OfferTokenOptions.SigningKey"/>. Token = base64url(<c>{offerId}.{expiresUnix}.{signature}</c>).
/// Validation is constant-time and rejects tokens issued for another offer or already expired.
/// </summary>
public sealed class HmacOfferResponseTokenService(OfferTokenOptions options) : IOfferResponseTokenService
{
    /// <summary>Upper bound on accepted token length; anything longer is rejected before decoding.</summary>
    public const int MaxTokenLength = 512;

    private readonly byte[] _key = Encoding.UTF8.GetBytes(options.SigningKey);

    public string Issue(long offerId, DateTimeOffset expiresAt)
    {
        if (offerId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offerId));
        }

        var expires = expiresAt.ToUnixTimeSeconds();
        var payload = string.Create(CultureInfo.InvariantCulture, $"{offerId}.{expires}.{Sign(offerId, expires)}");
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(payload));
    }

    public bool TryValidate(string? token, long offerId, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength || !Base64Url.IsValid(token, out var decodedLength))
        {
            return false;
        }

        var buffer = new byte[decodedLength];
        if (!Base64Url.TryDecodeFromChars(token, buffer, out var written))
        {
            return false;
        }

        var parts = Encoding.UTF8.GetString(buffer, 0, written).Split('.');
        if (parts.Length != 3 ||
            !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var tokenOfferId) ||
            !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var expires))
        {
            return false;
        }

        if (tokenOfferId != offerId || now.ToUnixTimeSeconds() > expires)
        {
            return false;
        }

        var expected = Encoding.ASCII.GetBytes(Sign(offerId, expires));
        var provided = Encoding.ASCII.GetBytes(parts[2]);
        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    private string Sign(long offerId, long expires)
    {
        var payload = Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{offerId}|{expires}"));
        return Base64Url.EncodeToString(HMACSHA256.HashData(_key, payload));
    }
}
