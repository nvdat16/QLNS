namespace Qlns.DataAccess.Modules.Recruitment.Offers;

/// <summary>
/// Configuration section <c>Recruitment</c> for the candidate response token. <c>OfferTokenSigningKey</c> is required,
/// must be a long random secret distinct from every other signing key, and is never logged.
/// </summary>
public sealed class OfferTokenOptions
{
    public const string SectionName = "Recruitment";
    public const string SigningKeyName = "OfferTokenSigningKey";

    /// <summary>HMAC secret used to sign <c>X-Offer-Token</c> values (<c>Recruitment:OfferTokenSigningKey</c>).</summary>
    public required string SigningKey { get; init; }
}
