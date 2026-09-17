using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

namespace Qlns.Api.Modules.Recruitment.Evaluations;

/// <summary>
/// Endpoint-level authorization policies for Recruitment → Evaluations. The blind-evaluation filter, panel membership
/// and the unlock permission are re-checked by <see cref="EvaluationService"/>.
/// </summary>
public static class EvaluationPolicies
{
    public const string Read = "RecruitmentEvaluationRead";
    public const string Submit = "RecruitmentEvaluationSubmit";
    public const string Unlock = "RecruitmentEvaluationUnlock";

    public static void AddEvaluationPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(Read, p => p.RequireClaim("permission", EvaluationPermissions.Read, EvaluationPermissions.ReadAll));
        options.AddPolicy(Submit, p => p.RequireClaim("permission", EvaluationPermissions.Submit));
        options.AddPolicy(Unlock, p => p.RequireClaim("permission", EvaluationPermissions.Unlock));
    }
}
