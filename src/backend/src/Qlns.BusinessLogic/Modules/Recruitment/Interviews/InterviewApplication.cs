namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>
/// The slice of an application (plus its job posting's department) the Interviews feature needs:
/// data scope is decided on <see cref="DepartmentId"/>, eligibility on <see cref="Stage"/>.
/// </summary>
public sealed record InterviewApplication(
    long Id,
    long CandidateId,
    long JobPostingId,
    long DepartmentId,
    string Stage);

/// <summary>
/// applications.stage values in which an interview may be scheduled. Kept local so this feature does not
/// depend on the Applications feature; the strings are the same contract values stored in the database.
/// </summary>
public static class InterviewApplicationStages
{
    public static readonly IReadOnlySet<string> Interviewable = new HashSet<string>(StringComparer.Ordinal)
    {
        "ai_screening", "tech_interview", "executive_round"
    };

    public static bool IsInterviewable(string stage) => Interviewable.Contains(stage);
}
