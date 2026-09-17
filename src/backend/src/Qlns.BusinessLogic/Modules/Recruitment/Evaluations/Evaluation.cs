using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>
/// One immutable scorecard version (table <c>evaluations</c>, REC-05.1). Rows are never updated: submitting
/// creates version 1, an HR Manager "unlock" copies the latest version into version+1 carrying the unlock
/// metadata, and the evaluator then submits fresh scores as the next version. <see cref="Version"/> is the ETag.
/// </summary>
public sealed class Evaluation
{
    public const int FeedbackMaxLength = 10000;
    public const int UnlockReasonMaxLength = 1000;

    public long Id { get; }
    public long InterviewId { get; }
    public long EvaluatorUserId { get; }
    public EvaluationScores Scores { get; }
    public decimal OverallScore { get; }
    public Recommendation Recommendation { get; }
    public string Feedback { get; }
    public DateTimeOffset SubmittedAt { get; }
    public DateTimeOffset? UnlockedAt { get; }
    public long? UnlockedBy { get; }
    public string? UnlockReason { get; }
    public int Version { get; }

    public bool IsUnlocked => UnlockedAt.HasValue;

    public Evaluation(
        long id,
        long interviewId,
        long evaluatorUserId,
        EvaluationScores scores,
        decimal overallScore,
        Recommendation recommendation,
        string feedback,
        DateTimeOffset submittedAt,
        DateTimeOffset? unlockedAt,
        long? unlockedBy,
        string? unlockReason,
        int version)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Identifier must be zero (transient) or positive.");
        }

        if (interviewId <= 0 || evaluatorUserId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(interviewId), "Persistent identifiers must be positive.");
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be at least 1.");
        }

        ArgumentNullException.ThrowIfNull(scores);
        ArgumentException.ThrowIfNullOrWhiteSpace(feedback);

        Id = id;
        InterviewId = interviewId;
        EvaluatorUserId = evaluatorUserId;
        Scores = scores;
        OverallScore = overallScore;
        Recommendation = recommendation;
        Feedback = feedback;
        SubmittedAt = submittedAt;
        UnlockedAt = unlockedAt;
        UnlockedBy = unlockedBy;
        UnlockReason = unlockReason;
        Version = version;
    }

    /// <summary>Validates the payload (422) and builds an unsaved scorecard with the given version.</summary>
    public static Evaluation Submit(
        long interviewId,
        long evaluatorUserId,
        EvaluationWrite write,
        int version,
        IEvaluationScoringPolicy scoringPolicy,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        ArgumentNullException.ThrowIfNull(scoringPolicy);

        var errors = new ValidationErrors();
        var scores = new EvaluationScores(write.TechnicalScore, write.CommunicationScore, write.ProblemSolvingScore, write.TeamworkScore);
        scores.Validate(errors);

        if (!RecommendationNames.TryParseContract(write.Recommendation, out var recommendation))
        {
            errors.Add("recommendation", "recommendation must be one of strong_hire, hire, hold, no_hire, strong_no_hire.");
        }

        var feedback = write.Feedback?.Trim();
        if (string.IsNullOrEmpty(feedback))
        {
            errors.Add("feedback", "feedback is required.");
        }
        else if (feedback.Length > FeedbackMaxLength)
        {
            errors.Add("feedback", $"feedback must be at most {FeedbackMaxLength} characters.");
        }

        errors.ThrowIfAny();

        return new Evaluation(
            id: 0,
            interviewId,
            evaluatorUserId,
            scores,
            scoringPolicy.ComputeOverall(scores),
            recommendation,
            feedback!,
            submittedAt: now,
            unlockedAt: null,
            unlockedBy: null,
            unlockReason: null,
            version);
    }

    /// <summary>
    /// The unlock record: a copy of this version as version+1 with the unlock metadata set. History stays untouched;
    /// the evaluator's next submission becomes version+2 with fresh scores.
    /// </summary>
    public Evaluation CreateUnlockedVersion(long unlockedBy, string? reason, DateTimeOffset now)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to unlock an evaluation.");
        }

        if (trimmed.Length > UnlockReasonMaxLength)
        {
            throw CoreHrValidationException.For("reason", $"reason must be at most {UnlockReasonMaxLength} characters.");
        }

        if (IsUnlocked)
        {
            throw new CoreHrBusinessRuleException(
                EvaluationService.AlreadyUnlockedCode,
                $"Evaluation {Id} (version {Version}) is already unlocked; wait for the evaluator to resubmit.");
        }

        return new Evaluation(
            id: 0,
            InterviewId,
            EvaluatorUserId,
            Scores,
            OverallScore,
            Recommendation,
            Feedback,
            SubmittedAt,
            unlockedAt: now,
            unlockedBy,
            trimmed,
            Version + 1);
    }
}
