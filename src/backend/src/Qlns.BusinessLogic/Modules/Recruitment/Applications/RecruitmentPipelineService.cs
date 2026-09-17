using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

/// <summary>
/// Use cases of REC-03.1/REC-03.2: read one application, build the Kanban pipeline of a requisition, advance an
/// application exactly one stage and end it with reject/withdraw. Data scope is enforced by the repository (404
/// outside scope); action-level permissions are re-checked here.
/// </summary>
public sealed class RecruitmentPipelineService(
    IRecruitmentApplicationRepository repository,
    TimeProvider timeProvider)
{
    private const string ResourceName = "Application";
    private const string RequisitionResourceName = "Requisition";
    private const string ConflictResource = "application";

    public const string AdvanceForbiddenCode = "recruitment.application.advance_forbidden";
    public const string TerminateForbiddenCode = "recruitment.application.terminate_forbidden";

    public async Task<RecruitmentApplication> GetAsync(
        long applicationId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return await repository.GetByIdAsync(applicationId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, applicationId);
    }

    /// <summary>The six active columns in order, or only the requested stage; each column is paged independently.</summary>
    public async Task<RecruitmentPipeline> GetPipelineAsync(
        RecruitmentPipelineQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);

        if (!await repository.IsRequisitionVisibleAsync(query.RequisitionId, actor, cancellationToken))
        {
            throw new CoreHrNotFoundException(RequisitionResourceName, query.RequisitionId);
        }

        IReadOnlyList<ApplicationStage> stages = query.Stage is { } requested
            ? [requested]
            : ApplicationStageNames.ActiveStages;

        var columns = new List<PipelineColumn>(stages.Count);
        foreach (var stage in stages)
        {
            columns.Add(await repository.GetPipelineColumnAsync(query, stage, cancellationToken));
        }

        return new RecruitmentPipeline(query.RequisitionId, columns);
    }

    public async Task<RecruitmentApplication> AdvanceAsync(
        AdvanceApplicationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        if (!actor.HasPermission(ApplicationPermissions.Advance))
        {
            throw new CoreHrForbiddenException(
                AdvanceForbiddenCode,
                "Advancing an application requires the recruitment.application.advance permission.");
        }

        var application = await LoadAsync(command.ApplicationId, command.ExpectedVersion, actor, cancellationToken);

        var eligibility = await repository.GetAdvanceEligibilityAsync(
            command.ApplicationId,
            command.TargetStage,
            cancellationToken);

        var previousStage = application.AdvanceTo(command.TargetStage, eligibility, timeProvider.GetUtcNow());

        var saved = await repository.SaveAdvanceAsync(
            application,
            previousStage,
            command.ExpectedVersion,
            string.IsNullOrWhiteSpace(command.Reason) ? null : command.Reason.Trim(),
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return application;
    }

    public async Task<RecruitmentApplication> TerminateAsync(
        TerminateApplicationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        if (!actor.HasPermission(ApplicationPermissions.Terminate))
        {
            throw new CoreHrForbiddenException(
                TerminateForbiddenCode,
                "Rejecting or withdrawing an application requires the recruitment.application.terminate permission.");
        }

        var application = await LoadAsync(command.ApplicationId, command.ExpectedVersion, actor, cancellationToken);

        var previousStage = application.Terminate(command.Action, command.Reason, timeProvider.GetUtcNow());

        var saved = await repository.SaveTerminationAsync(
            application,
            previousStage,
            command.ExpectedVersion,
            command.Reason!.Trim(),
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return application;
    }

    private async Task<RecruitmentApplication> LoadAsync(
        long applicationId,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var application = await repository.GetByIdAsync(applicationId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, applicationId);

        if (application.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return application;
    }
}
