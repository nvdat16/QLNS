using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.Api.Modules.Recruitment.Applications;

/// <summary>
/// Endpoint-level authorization policies for Recruitment → Pipeline. Each policy requires a <c>permission</c> claim;
/// data scope (the posting's department) is enforced by <see cref="RecruitmentPipelineService"/>.
/// </summary>
public static class ApplicationPolicies
{
    public const string Read = "RecruitmentApplicationRead";
    public const string Advance = "RecruitmentApplicationAdvance";
    public const string Terminate = "RecruitmentApplicationTerminate";

    public static void AddApplicationPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Read, p => p.RequireClaim("permission", ApplicationPermissions.Read));
        options.AddPolicy(Advance, p => p.RequireClaim("permission", ApplicationPermissions.Advance));
        options.AddPolicy(Terminate, p => p.RequireClaim("permission", ApplicationPermissions.Terminate));
    }
}
