namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed class RecruitmentPipelineService(
    IRecruitmentApplicationRepository repository,
    TimeProvider timeProvider)
{
    public async Task<RecruitmentApplication> GetAsync(
        long applicationId,
        RecruitmentDataScope dataScope,
        CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(applicationId, dataScope, cancellationToken)
            ?? throw new ApplicationNotFoundException(applicationId);
    }

    public async Task<RecruitmentApplication> AdvanceAsync(
        AdvanceApplicationCommand command,
        CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(command.ApplicationId, command.DataScope, cancellationToken)
            ?? throw new ApplicationNotFoundException(command.ApplicationId);

        if (application.Version != command.ExpectedVersion)
        {
            throw new ConcurrencyConflictException();
        }

        var eligibility = await repository.GetAdvanceEligibilityAsync(
            command.ApplicationId,
            command.TargetStage,
            cancellationToken);

        var previousStage = application.AdvanceTo(
            command.TargetStage,
            eligibility,
            timeProvider.GetUtcNow());

        var saved = await repository.SaveAdvanceAsync(
            application,
            previousStage,
            command.ExpectedVersion,
            command.ActorUserId,
            command.CorrelationId,
            command.Reason,
            cancellationToken);

        if (!saved)
        {
            throw new ConcurrencyConflictException();
        }

        return application;
    }
}
