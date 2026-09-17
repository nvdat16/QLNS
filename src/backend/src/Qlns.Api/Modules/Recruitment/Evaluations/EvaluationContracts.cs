using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

namespace Qlns.Api.Modules.Recruitment.Evaluations;

/// <summary>OpenAPI EvaluationWrite. Score range/step and the recommendation enum are validated by the domain (422).</summary>
public sealed record EvaluationWriteRequest(
    decimal TechnicalScore,
    decimal CommunicationScore,
    decimal ProblemSolvingScore,
    decimal TeamworkScore,
    [Required, MaxLength(30)] string Recommendation,
    [Required, MaxLength(Evaluation.FeedbackMaxLength)] string Feedback)
{
    public EvaluationWrite ToWrite() => new(
        TechnicalScore,
        CommunicationScore,
        ProblemSolvingScore,
        TeamworkScore,
        Recommendation,
        Feedback);
}

/// <summary>OpenAPI ReasonRequest (mandatory body of the unlock endpoint).</summary>
public sealed record ReasonRequest([Required, MaxLength(Evaluation.UnlockReasonMaxLength)] string Reason);

/// <summary>OpenAPI Evaluation. Unlock metadata beyond <see cref="UnlockedAt"/> stays in the audit trail.</summary>
public sealed record EvaluationResponse(
    long Id,
    long InterviewId,
    long EvaluatorUserId,
    decimal TechnicalScore,
    decimal CommunicationScore,
    decimal ProblemSolvingScore,
    decimal TeamworkScore,
    decimal OverallScore,
    string Recommendation,
    string Feedback,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? UnlockedAt,
    int Version)
{
    public static EvaluationResponse From(Evaluation evaluation) => new(
        evaluation.Id,
        evaluation.InterviewId,
        evaluation.EvaluatorUserId,
        evaluation.Scores.Technical,
        evaluation.Scores.Communication,
        evaluation.Scores.ProblemSolving,
        evaluation.Scores.Teamwork,
        evaluation.OverallScore,
        evaluation.Recommendation.ToContract(),
        evaluation.Feedback,
        evaluation.SubmittedAt,
        evaluation.UnlockedAt,
        evaluation.Version);
}
