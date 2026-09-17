using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>
/// Persistence contract for requisitions (job_postings). Implementations apply the actor's data scope on every read
/// (organization-wide, or the posting's department in the actor's set; self-only actors see nothing) and write the
/// business change together with its audit row (and outbox message) in one transaction.
/// </summary>
public interface IRequisitionRepository
{
    Task<PagedResult<Requisition>> SearchAsync(
        RequisitionSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    Task<Requisition?> GetByIdAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken);

    Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken);

    Task<bool> PositionExistsAsync(long positionId, CancellationToken cancellationToken);

    /// <summary>True while any offer with status <c>sent</c> belongs to an application of the posting.</summary>
    Task<bool> HasOpenOffersAsync(long requisitionId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the draft, assigns its server-generated <see cref="Requisition.JobCode"/> and writes the audit row in
    /// one transaction; returns the persisted requisition.
    /// </summary>
    Task<Requisition> InsertAsync(Requisition draft, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>id AND version = expectedVersion</c> plus audit row; false when no row matched.</summary>
    Task<bool> ReplaceAsync(
        Requisition requisition,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Conditional update on <c>id AND version = expectedVersion</c> plus audit row (and the publication outbox
    /// message when <paramref name="action"/> is publish); false when no row matched.
    /// </summary>
    Task<bool> SaveTransitionAsync(
        Requisition requisition,
        RequisitionStatus previousStatus,
        RequisitionAction action,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
