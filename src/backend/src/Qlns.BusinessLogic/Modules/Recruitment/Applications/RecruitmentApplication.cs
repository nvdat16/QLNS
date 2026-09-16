namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed class RecruitmentApplication
{
    private static readonly ApplicationStage[] ActiveStages =
    [
        ApplicationStage.SourcedApplied,
        ApplicationStage.AiScreening,
        ApplicationStage.TechInterview,
        ApplicationStage.ExecutiveRound,
        ApplicationStage.OfferLetter,
        ApplicationStage.HiredReady
    ];

    public long Id { get; }
    public long CandidateId { get; }
    public long JobPostingId { get; }
    public ApplicationStage Stage { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public RecruitmentApplication(
        long id,
        long candidateId,
        long jobPostingId,
        ApplicationStage stage,
        long version,
        DateTimeOffset updatedAt)
    {
        if (id <= 0 || candidateId <= 0 || jobPostingId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Id = id;
        CandidateId = candidateId;
        JobPostingId = jobPostingId;
        Stage = stage;
        Version = version;
        UpdatedAt = updatedAt;
    }

    public ApplicationStage AdvanceTo(
        ApplicationStage target,
        AdvanceEligibility eligibility,
        DateTimeOffset occurredAt)
    {
        var currentIndex = Array.IndexOf(ActiveStages, Stage);
        var targetIndex = Array.IndexOf(ActiveStages, target);

        if (currentIndex < 0 || targetIndex != currentIndex + 1)
        {
            throw new BusinessRuleException(
                "recruitment.invalid_stage_transition",
                "Applications can advance only one active stage at a time.");
        }

        if (target == ApplicationStage.TechInterview && !eligibility.HasScheduledInterview)
        {
            throw new BusinessRuleException(
                "recruitment.interview_required",
                "A scheduled interview is required before entering tech_interview.");
        }

        if (target == ApplicationStage.OfferLetter && !eligibility.HasEligibleEvaluation)
        {
            throw new BusinessRuleException(
                "recruitment.evaluation_required",
                "An eligible submitted evaluation is required before entering offer_letter.");
        }

        var previous = Stage;
        Stage = target;
        Version++;
        UpdatedAt = occurredAt;
        return previous;
    }
}
