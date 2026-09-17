using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

/// <summary>
/// One candidate's application to one job posting (REC-03, table applications). Advances exactly one active stage
/// at a time subject to the interview/evaluation prerequisites; reject and withdraw end it from any active stage
/// with a mandatory reason. Every successful mutation bumps <see cref="Version"/> and sets <see cref="UpdatedAt"/>.
/// </summary>
public sealed class RecruitmentApplication
{
    public const string DefaultSource = "direct";
    public const int SourceMaxLength = 80;
    public const int ReasonMaxLength = 1000;

    public const string InvalidStageTransitionCode = "recruitment.application.invalid_stage_transition";
    public const string InterviewRequiredCode = "recruitment.application.interview_required";
    public const string EvaluationRequiredCode = "recruitment.application.evaluation_required";

    public long Id { get; }
    public long CandidateId { get; }
    public long JobPostingId { get; }
    public long? ResumeId { get; }
    public ApplicationStage Stage { get; private set; }
    public decimal? AiScore { get; }
    public string Source { get; }
    public DateTimeOffset AppliedAt { get; }
    public long Version { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public RecruitmentApplication(
        long id,
        long candidateId,
        long jobPostingId,
        long? resumeId,
        ApplicationStage stage,
        decimal? aiScore,
        string source,
        DateTimeOffset appliedAt,
        long version,
        DateTimeOffset updatedAt)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Identifier must be zero (transient) or positive.");
        }

        if (candidateId <= 0 || jobPostingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateId), "Persistent identifiers must be positive.");
        }

        if (resumeId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(resumeId), "Persistent identifiers must be positive.");
        }

        if (aiScore is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(aiScore), "aiScore must be between 0 and 100.");
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be at least 1.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Id = id;
        CandidateId = candidateId;
        JobPostingId = jobPostingId;
        ResumeId = resumeId;
        Stage = stage;
        AiScore = aiScore;
        Source = source;
        AppliedAt = appliedAt;
        Version = version;
        UpdatedAt = updatedAt;
    }

    /// <summary>Transient application in <c>sourced_applied</c> created by intake confirmation (REC-02.2). AI score is assigned later.</summary>
    public static RecruitmentApplication Create(
        long candidateId,
        long jobPostingId,
        long? resumeId,
        string source,
        DateTimeOffset now) => new(
        id: 0,
        candidateId,
        jobPostingId,
        resumeId,
        ApplicationStage.SourcedApplied,
        aiScore: null,
        source,
        appliedAt: now,
        version: 1,
        updatedAt: now);

    /// <summary>Trims the client-supplied source, applies the contract default and validates its length (422).</summary>
    public static string NormalizeSource(string? source)
    {
        var normalized = string.IsNullOrWhiteSpace(source) ? DefaultSource : source.Trim();
        if (normalized.Length > SourceMaxLength)
        {
            throw CoreHrValidationException.For("source", $"source must be at most {SourceMaxLength} characters.");
        }

        return normalized;
    }

    /// <summary>Moves exactly one active stage forward; returns the previous stage.</summary>
    public ApplicationStage AdvanceTo(ApplicationStage target, AdvanceEligibility eligibility, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(eligibility);

        var active = ApplicationStageNames.ActiveStages;
        var currentIndex = IndexOf(active, Stage);
        var targetIndex = IndexOf(active, target);

        if (currentIndex < 0 || targetIndex != currentIndex + 1)
        {
            throw new CoreHrBusinessRuleException(
                InvalidStageTransitionCode,
                "Applications can advance only one active stage at a time.")
            {
                Details = TransitionDetails(target.ToContract())
            };
        }

        if (target == ApplicationStage.TechInterview && !eligibility.HasScheduledInterview)
        {
            throw new CoreHrBusinessRuleException(
                InterviewRequiredCode,
                "A scheduled interview is required before entering tech_interview.");
        }

        if (target == ApplicationStage.OfferLetter && !eligibility.HasEligibleEvaluation)
        {
            throw new CoreHrBusinessRuleException(
                EvaluationRequiredCode,
                "An eligible submitted evaluation from a completed interview is required before entering offer_letter.");
        }

        var previous = Stage;
        Stage = target;
        Touch(occurredAt);
        return previous;
    }

    /// <summary>Any active stage → rejected with a mandatory reason.</summary>
    public ApplicationStage Reject(string? reason, DateTimeOffset occurredAt) =>
        Terminate(ApplicationTerminalAction.Reject, reason, occurredAt);

    /// <summary>Any active stage → withdrawn with a mandatory reason.</summary>
    public ApplicationStage Withdraw(string? reason, DateTimeOffset occurredAt) =>
        Terminate(ApplicationTerminalAction.Withdraw, reason, occurredAt);

    /// <summary>Applies a terminal action and returns the previous stage. Terminal applications cannot be terminated again.</summary>
    public ApplicationStage Terminate(ApplicationTerminalAction action, string? reason, DateTimeOffset occurredAt)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", $"A reason is required to {action.ToContract()} an application.");
        }

        if (reason.Trim().Length > ReasonMaxLength)
        {
            throw CoreHrValidationException.For("reason", $"reason must be at most {ReasonMaxLength} characters.");
        }

        if (!Stage.IsActive())
        {
            throw new CoreHrBusinessRuleException(
                InvalidStageTransitionCode,
                $"Cannot {action.ToContract()} an application that is already {Stage.ToContract()}.")
            {
                Details = TransitionDetails(action.ToContract())
            };
        }

        var previous = Stage;
        Stage = action.ToStage();
        Touch(occurredAt);
        return previous;
    }

    private Dictionary<string, object?> TransitionDetails(string action) => new()
    {
        ["currentStage"] = Stage.ToContract(),
        ["action"] = action
    };

    private static int IndexOf(IReadOnlyList<ApplicationStage> stages, ApplicationStage stage)
    {
        for (var index = 0; index < stages.Count; index++)
        {
            if (stages[index] == stage)
            {
                return index;
            }
        }

        return -1;
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }
}
