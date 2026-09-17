namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>
/// Permission claim values of Core HR → Probation (EMP-06). Endpoint policies require the coarse claim;
/// <see cref="ProbationReviewService"/> re-checks reviewer identity, <see cref="Manage"/> and <see cref="Decide"/>.
/// </summary>
public static class ProbationPermissions
{
    public const string Read = "corehr.probation.read";
    public const string Manage = "corehr.probation.manage";
    public const string Decide = "corehr.probation.decide";
}
