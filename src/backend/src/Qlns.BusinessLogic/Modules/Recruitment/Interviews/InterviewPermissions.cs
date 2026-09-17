namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>Permission claim values of the Recruitment → Interviews feature (REC-04.1).</summary>
public static class InterviewPermissions
{
    /// <summary>Search and read interviews inside the actor's data scope or on their own panel.</summary>
    public const string Read = "recruitment.interview.read";

    /// <summary>Schedule, reschedule, complete and cancel interviews (Recruiter).</summary>
    public const string Manage = "recruitment.interview.manage";
}
