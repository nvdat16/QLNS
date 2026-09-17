namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>Permission claim values of the Recruitment → Offers feature (REC-06.1 / REC-06.2).</summary>
public static class OfferPermissions
{
    /// <summary>Search and read offers inside the actor's data scope.</summary>
    public const string Read = "recruitment.offer.read";

    /// <summary>Draft, send and cancel offers (Recruiter).</summary>
    public const string Write = "recruitment.offer.write";

    /// <summary>Approve an offer and re-approve it when extending its expiration (HR Manager).</summary>
    public const string Approve = "recruitment.offer.approve";
}
