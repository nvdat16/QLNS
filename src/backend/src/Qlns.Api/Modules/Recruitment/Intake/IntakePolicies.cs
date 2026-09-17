using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;

namespace Qlns.Api.Modules.Recruitment.Intake;

/// <summary>
/// Endpoint-level authorization policies for Recruitment → Intake. Each policy requires a <c>permission</c> claim;
/// data scope (the requisition's department) is enforced by <see cref="CandidateIntakeService"/>.
/// </summary>
public static class IntakePolicies
{
    public const string Read = "RecruitmentIntakeRead";
    public const string Write = "RecruitmentIntakeWrite";

    public static void AddIntakePolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Read, p => p.RequireClaim("permission", IntakePermissions.Read, IntakePermissions.Write));
        options.AddPolicy(Write, p => p.RequireClaim("permission", IntakePermissions.Write));
    }
}
