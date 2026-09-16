namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed record AdvanceEligibility(
    bool HasScheduledInterview,
    bool HasEligibleEvaluation);
