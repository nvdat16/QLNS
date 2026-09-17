using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

/// <summary>
/// Persistence contract for applications. Implementations apply the actor's data scope on every read through the
/// job posting's department (organization-wide, or department in the actor's set; self-only actors see nothing) and
/// write the stage change, its application_stage_events row, the audit row and any outbox message in one transaction.
/// </summary>
public interface IRecruitmentApplicationRepository
{
    Task<RecruitmentApplication?> GetByIdAsync(
        long applicationId,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>True when the job posting exists and its department is in the actor's data scope.</summary>
    Task<bool> IsRequisitionVisibleAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>One Kanban column: count, average AI score and one page of cards after the query filters, newest application first.</summary>
    Task<PipelineColumn> GetPipelineColumnAsync(
        RecruitmentPipelineQuery query,
        ApplicationStage stage,
        CancellationToken cancellationToken);

    Task<AdvanceEligibility> GetAdvanceEligibilityAsync(
        long applicationId,
        ApplicationStage targetStage,
        CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>id AND version = expectedVersion</c> plus stage event and audit row; false when no row matched.</summary>
    Task<bool> SaveAdvanceAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conditional update on <c>id AND version = expectedVersion</c> plus stage event, audit row and, for a rejection,
    /// the <c>recruitment.application.rejected</c> outbox message (thank-you e-mail); false when no row matched.
    /// </summary>
    Task<bool> SaveTerminationAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        string reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
