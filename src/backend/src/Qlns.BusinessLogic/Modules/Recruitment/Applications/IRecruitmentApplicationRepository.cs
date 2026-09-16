namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public interface IRecruitmentApplicationRepository
{
    Task<RecruitmentApplication?> GetByIdAsync(
        long applicationId,
        RecruitmentDataScope dataScope,
        CancellationToken cancellationToken);

    Task<AdvanceEligibility> GetAdvanceEligibilityAsync(
        long applicationId,
        ApplicationStage targetStage,
        CancellationToken cancellationToken);

    Task<bool> SaveAdvanceAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        long actorUserId,
        string correlationId,
        string? reason,
        CancellationToken cancellationToken);
}
