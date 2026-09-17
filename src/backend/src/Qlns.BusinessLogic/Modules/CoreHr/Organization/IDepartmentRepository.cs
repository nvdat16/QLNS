using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

public interface IDepartmentRepository
{
    Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken);

    Task<Department?> GetByIdAsync(long departmentId, CancellationToken cancellationToken);

    /// <summary>Headcount (employees with status other than <c>terminated</c>) keyed by department id.</summary>
    Task<IReadOnlyDictionary<long, int>> GetHeadcountsAsync(CancellationToken cancellationToken);

    /// <summary>Case-insensitive code uniqueness check, optionally ignoring one department.</summary>
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(long departmentId, CancellationToken cancellationToken);

    Task<DepartmentDependencies> GetDependenciesAsync(long departmentId, CancellationToken cancellationToken);

    /// <summary>Inserts the department and its audit row in one transaction; returns the generated id.</summary>
    Task<long> InsertAsync(Department department, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>Version == expectedVersion</c>; false when no row matched.</summary>
    Task<bool> ReplaceAsync(Department department, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Conditional delete on <c>Version == expectedVersion</c>; false when no row matched.</summary>
    Task<bool> DeleteAsync(long departmentId, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken);
}
