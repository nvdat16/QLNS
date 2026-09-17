namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>Client payload of OpenAPI ProbationReviewWrite. Validated by <see cref="ProbationReview.Submit"/>.</summary>
public sealed record ProbationReviewWrite(
    decimal? OverallScore,
    string? Strengths,
    string? Improvements,
    string? RecommendedOutcome);

/// <summary>Client payload of OpenAPI ProbationDecisionRequest (optional body of the transition endpoint).</summary>
public sealed record ProbationDecision(
    string? Outcome,
    DateOnly? EffectiveDate,
    string? Reason)
{
    public static ProbationDecision Empty { get; } = new(null, null, null);
}
