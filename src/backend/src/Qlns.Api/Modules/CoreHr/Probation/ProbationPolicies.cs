using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;

namespace Qlns.Api.Modules.CoreHr.Probation;

/// <summary>
/// Endpoint-level authorization policies of Core HR → Probation (EMP-06). Each policy requires a
/// <c>permission</c> claim; reviewer identity, data scope and the decide/manage split are enforced by
/// <see cref="ProbationReviewService"/>.
/// </summary>
public static class ProbationPolicies
{
    public const string Read = "CoreHrProbationRead";
    public const string Manage = "CoreHrProbationManage";

    public static void AddProbationPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddPolicy(Read, p => p.RequireClaim("permission", ProbationPermissions.Read));
        options.AddPolicy(Manage, p => p.RequireClaim("permission",
            ProbationPermissions.Manage, ProbationPermissions.Decide));
    }
}
