using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.Api.Modules.Recruitment.Requisitions;

/// <summary>
/// Endpoint-level authorization policies for Recruitment → Requisitions. Each policy requires a <c>permission</c>
/// claim; data scope and the per-action permissions (approve, publish, cancel) are enforced by <see cref="RequisitionService"/>.
/// </summary>
public static class RequisitionPolicies
{
    public const string Read = "RecruitmentRequisitionRead";
    public const string Write = "RecruitmentRequisitionWrite";

    /// <summary>Any workflow claim may enter the transition endpoint; the service decides per action.</summary>
    public const string Transition = "RecruitmentRequisitionTransition";

    public static void AddRequisitionPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Read, p => p.RequireClaim("permission", RequisitionPermissions.Read));
        options.AddPolicy(Write, p => p.RequireClaim("permission", RequisitionPermissions.Write));
        options.AddPolicy(Transition, p => p.RequireClaim("permission",
            RequisitionPermissions.Write, RequisitionPermissions.Approve, RequisitionPermissions.Publish));
    }
}
