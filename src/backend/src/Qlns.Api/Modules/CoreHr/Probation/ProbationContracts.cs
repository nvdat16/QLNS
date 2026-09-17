using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;

namespace Qlns.Api.Modules.CoreHr.Probation;

/// <summary>OpenAPI ProbationReviewWrite. Semantic rules (range, one decimal, termination notes) are enforced by the domain.</summary>
public sealed record ProbationReviewWriteRequest(
    [Required] decimal? OverallScore,
    [Required, MaxLength(ProbationReview.TextMaxLength)] string Strengths,
    [MaxLength(ProbationReview.TextMaxLength)] string? Improvements,
    [Required, MaxLength(30)] string RecommendedOutcome)
{
    public ProbationReviewWrite ToWrite() => new(OverallScore, Strengths, Improvements, RecommendedOutcome);
}

/// <summary>OpenAPI ProbationDecisionRequest (optional body of the transition endpoint).</summary>
public sealed record ProbationDecisionRequest(
    [MaxLength(30)] string? Outcome,
    DateOnly? EffectiveDate,
    [MaxLength(1000)] string? Reason)
{
    public ProbationDecision ToDecision() => new(Outcome, EffectiveDate, Reason);
}

/// <summary>OpenAPI ProbationReview.</summary>
public sealed record ProbationReviewResponse(
    long Id,
    long EmployeeId,
    long ContractId,
    DateOnly ReviewDueDate,
    bool Overdue,
    long? ReviewerUserId,
    string Status,
    string? Outcome,
    decimal? OverallScore,
    string? Strengths,
    string? Improvements,
    DateOnly? EffectiveDate,
    long? DecidedBy,
    DateTimeOffset? DecidedAt,
    long? EmployeeEventId,
    long Version,
    DateTimeOffset CreatedAt)
{
    public static ProbationReviewResponse From(ProbationReviewView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        var review = view.Review;
        return new ProbationReviewResponse(
            review.Id,
            review.EmployeeId,
            review.ContractId,
            review.ReviewDueDate,
            view.Overdue,
            review.ReviewerUserId,
            review.Status.ToContract(),
            review.Outcome?.ToContract(),
            review.OverallScore,
            review.Strengths,
            review.Improvements,
            review.EffectiveDate,
            review.DecidedBy,
            review.DecidedAt,
            review.EmployeeEventId,
            review.Version,
            review.CreatedAt);
    }
}
