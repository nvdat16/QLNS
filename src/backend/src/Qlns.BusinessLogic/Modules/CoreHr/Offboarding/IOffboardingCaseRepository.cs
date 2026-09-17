using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Persistence contract for offboarding cases. Searches apply the actor's data scope in SQL (organization,
/// department of the case's employee, or the actor's own record); single reads return the employee snapshot so
/// the service can decide scope. Every write pairs the business change with its audit (and outbox) rows in one transaction.
/// </summary>
public interface IOffboardingCaseRepository
{
    Task<OffboardingEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>notice_period_days of the employee's primary executed/active contract; null when none or undefined.</summary>
    Task<int?> GetNoticePeriodDaysAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>True when the employee has a case in draft, pending_approval, approved or in_progress.</summary>
    Task<bool> HasOpenCaseAsync(long employeeId, CancellationToken cancellationToken);

    Task<PagedResult<OffboardingCaseEntry>> SearchAsync(
        OffboardingCaseSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    Task<OffboardingCaseEntry?> GetByIdAsync(long caseId, CancellationToken cancellationToken);

    /// <summary>All tasks of the case with blocks_last_working_day = true, whatever their status.</summary>
    Task<IReadOnlyList<OffboardingTask>> ListBlockingTasksAsync(long caseId, CancellationToken cancellationToken);

    /// <summary>template_key values already present on the case (idempotent checklist generation).</summary>
    Task<IReadOnlySet<string>> ListTemplateKeysAsync(long caseId, CancellationToken cancellationToken);

    /// <summary>
    /// Inserts the draft and its audit row (which records the notice-period warning) in one transaction.
    /// A violation of ux_offboarding_open_case surfaces as <see cref="CoreHrBusinessRuleException"/> with
    /// code <see cref="OffboardingCase.CaseOpenCode"/>.
    /// </summary>
    Task<OffboardingCase> InsertAsync(
        OffboardingCase offboardingCase,
        int? noticePeriodShortfallDays,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Conditional update to approved plus insertion of the generated tasks; false when the version did not match.</summary>
    Task<bool> SaveApprovalAsync(
        OffboardingCase offboardingCase,
        OffboardingCaseStatus previousStatus,
        IReadOnlyList<OffboardingTask> generatedTasks,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>start | cancel: conditional status update plus audit row; false when the version did not match.</summary>
    Task<bool> SaveTransitionAsync(
        OffboardingCase offboardingCase,
        OffboardingCaseStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// complete: conditional status update, insertion of the approved termination event (when supplied) linked
    /// through employee_event_id, audit row (with <paramref name="overrideReason"/> when blocking tasks were
    /// overridden) and outbox message. False when the version did not match.
    /// </summary>
    Task<bool> SaveCompletionAsync(
        OffboardingCase offboardingCase,
        ApprovedEmployeeEvent? terminationEvent,
        long expectedVersion,
        string? overrideReason,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
