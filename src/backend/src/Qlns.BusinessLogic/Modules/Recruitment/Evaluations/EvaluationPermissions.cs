namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>Permission claim values of the Recruitment → Evaluations feature (REC-05.1).</summary>
public static class EvaluationPermissions
{
    /// <summary>Read scorecards of a visible interview, subject to the blind-evaluation policy.</summary>
    public const string Read = "recruitment.evaluation.read";

    /// <summary>Read every scorecard version regardless of the blind-evaluation policy (Recruiter / HR Manager).</summary>
    public const string ReadAll = "recruitment.evaluation.read_all";

    /// <summary>Submit a scorecard as a panelist (Interviewer).</summary>
    public const string Submit = "recruitment.evaluation.submit";

    /// <summary>Unlock a submitted scorecard by creating a new auditable version (HR Manager).</summary>
    public const string Unlock = "recruitment.evaluation.unlock";
}
