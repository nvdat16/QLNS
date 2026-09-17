using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>
/// Persistence contract for probation reviews. Rows are created by the Contracts module when a probation
/// contract is activated; this feature only reads and updates them. Searches apply the actor's data scope in SQL;
/// single reads return the employee's department so the service can decide scope. Writes pair the business change
/// with audit (and outbox) rows in one transaction and return false when the expected version did not match.
/// </summary>
public interface IProbationReviewRepository
{
    /// <summary>Department of the employee, or null when the employee does not exist.</summary>
    Task<long?> GetEmployeeDepartmentAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>Newest non-cancelled review of the employee whose contract is of type probation.</summary>
    Task<ProbationReviewEntry?> GetCurrentForEmployeeAsync(long employeeId, CancellationToken cancellationToken);

    Task<ProbationReviewEntry?> GetByIdAsync(long reviewId, CancellationToken cancellationToken);

    Task<PagedResult<ProbationReview>> SearchAsync(
        ProbationReviewSearchQuery query,
        CoreHrActor actor,
        DateOnly today,
        CancellationToken cancellationToken);

    /// <summary>Status of the employee event linked to a decided review; null when the event does not exist.</summary>
    Task<EmployeeEventStatus?> GetEmployeeEventStatusAsync(long employeeEventId, CancellationToken cancellationToken);

    /// <summary>PUT assessment: conditional update, audit row and <c>corehr.probation.review_submitted</c> outbox message.</summary>
    Task<bool> SaveAssessmentAsync(
        ProbationReview review,
        ProbationReviewStatus previousStatus,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// decide: conditional update guarded by <c>version = expected AND status &lt;&gt; 'decided' AND employee_event_id IS NULL</c>,
    /// insertion of the approved employee event (linked through employee_event_id) and of the offboarding case when
    /// the plan carries one and the employee has no open case, plus audit rows. False when the guard matched no row.
    /// </summary>
    Task<bool> SaveDecisionAsync(
        ProbationReview review,
        ProbationDecisionPlan plan,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>cancel: conditional status update plus audit row carrying the reason.</summary>
    Task<bool> SaveCancellationAsync(
        ProbationReview review,
        ProbationReviewStatus previousStatus,
        long expectedVersion,
        string reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// unlock: conditional update clearing the decision columns, cancellation of the linked employee event
    /// (guarded on its status not being applied) and audit rows carrying the reason.
    /// </summary>
    Task<bool> SaveUnlockAsync(
        ProbationReview review,
        long employeeEventId,
        long expectedVersion,
        string reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
