using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.Api.Modules.Recruitment.Offers;

/// <summary>
/// Endpoint-level authorization policies for Recruitment → Offers. <see cref="Write"/> admits recruiters (write claim)
/// and HR Managers (approve claim) to the transition endpoint; <see cref="OfferService"/> decides per action which claim
/// is required (approve / extend need the approve claim). The candidate response endpoint is anonymous and authenticated
/// by <c>X-Offer-Token</c> instead.
/// </summary>
public static class OfferPolicies
{
    public const string Read = "RecruitmentOfferRead";
    public const string Write = "RecruitmentOfferWrite";

    public static void AddOfferPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(Read, p => p.RequireClaim("permission",
            OfferPermissions.Read, OfferPermissions.Write, OfferPermissions.Approve));
        options.AddPolicy(Write, p => p.RequireClaim("permission",
            OfferPermissions.Write, OfferPermissions.Approve));
    }
}
