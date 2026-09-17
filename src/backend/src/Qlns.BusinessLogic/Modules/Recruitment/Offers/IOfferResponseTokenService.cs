namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>
/// Port for the single-offer candidate token carried in <c>X-Offer-Token</c> (OpenAPI security scheme offerToken).
/// A token is bound to one offer id and an absolute expiry; the adapter decides the signing scheme.
/// </summary>
public interface IOfferResponseTokenService
{
    string Issue(long offerId, DateTimeOffset expiresAt);

    /// <summary>True only when the token is well-formed, signed for <paramref name="offerId"/> and not expired at <paramref name="now"/>.</summary>
    bool TryValidate(string? token, long offerId, DateTimeOffset now);
}

/// <summary>Lifetime rule of the response token: the end of the offer's expiration date (UTC) plus one day of grace.</summary>
public static class OfferResponseTokenLifetime
{
    public const int GraceDays = 1;

    public static DateTimeOffset ExpiresAt(DateOnly expirationDate) =>
        new(expirationDate.AddDays(1 + GraceDays).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
}
