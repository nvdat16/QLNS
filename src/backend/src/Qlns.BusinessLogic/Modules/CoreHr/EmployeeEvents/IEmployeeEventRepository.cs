using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

public interface IEmployeeEventRepository
{
    Task<EmployeeMasterData?> GetEmployeeMasterDataAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>Ordered by effective_date desc, id desc.</summary>
    Task<PagedResult<EmployeeEvent>> ListByEmployeeAsync(
        long employeeId,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<EmployeeEvent?> GetByIdAsync(long eventId, CancellationToken cancellationToken);

    /// <summary>Non-cancelled events of the employee on <paramref name="effectiveDate"/> sharing any of <paramref name="fields"/>.</summary>
    Task<IReadOnlyList<ConflictingEvent>> FindConflictingAsync(
        long employeeId,
        DateOnly effectiveDate,
        IReadOnlyCollection<string> fields,
        long? excludeEventId,
        CancellationToken cancellationToken);

    /// <summary>Inserts the draft and its audit row in one transaction; returns the event with its generated id.</summary>
    Task<EmployeeEvent> InsertAsync(EmployeeEvent employeeEvent, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>id AND version = expectedVersion</c> plus audit row; false when no row matched.</summary>
    Task<bool> SaveTransitionAsync(
        EmployeeEvent employeeEvent,
        EmployeeEventStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Approved events whose effective date is on or before <paramref name="today"/>.</summary>
    Task<IReadOnlyList<EmployeeEvent>> ListDueApprovedAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>
    /// One transaction: mark the event applied (conditional on its version) and write the new master data to
    /// employees (conditional on the employee version). False, with rollback, when either update matched no row.
    /// </summary>
    Task<bool> SaveAppliedAsync(
        EmployeeEvent employeeEvent,
        long expectedEventVersion,
        EmployeeMasterData newData,
        long expectedEmployeeVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
