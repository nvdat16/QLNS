namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>offers.status (ck_offer_status). Contract values are snake_case.</summary>
public enum OfferStatus
{
    Draft,
    Approved,
    Sent,
    Accepted,
    Declined,
    Expired,
    Cancelled
}

public static class OfferStatusNames
{
    /// <summary>Statuses covered by <c>ux_offers_one_open_per_application</c>: at most one such offer per application.</summary>
    public static readonly IReadOnlySet<OfferStatus> Open = new HashSet<OfferStatus>
    {
        OfferStatus.Draft, OfferStatus.Approved, OfferStatus.Sent, OfferStatus.Accepted
    };

    public static string ToContract(this OfferStatus status) => status switch
    {
        OfferStatus.Draft => "draft",
        OfferStatus.Approved => "approved",
        OfferStatus.Sent => "sent",
        OfferStatus.Accepted => "accepted",
        OfferStatus.Declined => "declined",
        OfferStatus.Expired => "expired",
        OfferStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out OfferStatus status)
    {
        status = value switch
        {
            "draft" => OfferStatus.Draft,
            "approved" => OfferStatus.Approved,
            "sent" => OfferStatus.Sent,
            "accepted" => OfferStatus.Accepted,
            "declined" => OfferStatus.Declined,
            "expired" => OfferStatus.Expired,
            "cancelled" => OfferStatus.Cancelled,
            _ => default
        };

        return value is "draft" or "approved" or "sent" or "accepted" or "declined" or "expired" or "cancelled";
    }
}
