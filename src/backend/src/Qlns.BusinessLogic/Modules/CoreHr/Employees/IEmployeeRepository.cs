using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

public interface IEmployeeRepository
{
    /// <summary>Scope-filtered directory page. Records outside the actor's data scope are never returned.</summary>
    Task<PagedResult<Employee>> SearchAsync(
        EmployeeSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Returns null when the employee does not exist or is outside the actor's data scope.</summary>
    Task<Employee?> GetByIdAsync(
        long employeeId,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists the personal fields and audit row in one transaction, conditioned on
    /// <paramref name="expectedVersion"/>. Returns false when a concurrent write won.
    /// </summary>
    Task<bool> SavePersonalProfileAsync(
        Employee employee,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
