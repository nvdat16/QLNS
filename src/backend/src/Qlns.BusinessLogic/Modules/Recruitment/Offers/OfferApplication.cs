namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>
/// The slice of an application (plus its job posting's department) the Offers feature needs for scope and
/// eligibility checks when drafting an offer.
/// </summary>
public sealed record OfferApplication(
    long Id,
    long CandidateId,
    long JobPostingId,
    long DepartmentId,
    string Stage);

/// <summary>
/// applications.stage values the offer workflow reads and writes. Kept local so this feature does not depend on
/// the Applications feature; the strings are the same contract values stored in the database.
/// </summary>
public static class OfferApplicationStages
{
    /// <summary>The only stage in which an offer may be drafted and later accepted.</summary>
    public const string OfferLetter = "offer_letter";

    /// <summary>Stage the application enters when the candidate accepts (REC-06.2).</summary>
    public const string HiredReady = "hired_ready";
}
