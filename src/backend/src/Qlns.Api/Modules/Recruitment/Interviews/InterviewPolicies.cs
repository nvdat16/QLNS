using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;

namespace Qlns.Api.Modules.Recruitment.Interviews;

/// <summary>
/// Endpoint-level authorization policies for Recruitment → Interviews. <see cref="Read"/> admits interviewers
/// (read claim) and recruiters (manage claim); the transition endpoint uses it so a panelist can complete their own
/// interview, while <see cref="InterviewService"/> requires the manage claim for reschedule and cancel.
/// </summary>
public static class InterviewPolicies
{
    public const string Read = "RecruitmentInterviewRead";
    public const string Manage = "RecruitmentInterviewManage";

    public static void AddInterviewPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(Read, p => p.RequireClaim("permission", InterviewPermissions.Read, InterviewPermissions.Manage));
        options.AddPolicy(Manage, p => p.RequireClaim("permission", InterviewPermissions.Manage));
    }
}
