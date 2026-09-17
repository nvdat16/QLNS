using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

public interface IEmployeeDocumentRepository
{
    /// <summary>Department of the employee, or null when the employee does not exist.</summary>
    Task<long?> GetEmployeeDepartmentAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>All non-deleted document versions of the employee, without access filtering.</summary>
    Task<IReadOnlyList<EmployeeDocument>> ListByEmployeeAsync(long employeeId, CancellationToken cancellationToken);

    /// <summary>Document (including soft-deleted rows) with the department of its owner, or null.</summary>
    Task<(EmployeeDocument Document, long EmployeeDepartmentId)?> GetByIdAsync(
        long documentId,
        CancellationToken cancellationToken);

    /// <summary>Highest stored version for the type, including soft-deleted rows; 0 when none.</summary>
    Task<int> GetLatestVersionAsync(long employeeId, string documentType, CancellationToken cancellationToken);

    /// <summary>Inserts the document row and its audit row in one transaction; returns the persisted document.</summary>
    Task<EmployeeDocument> InsertAsync(EmployeeDocument document, CoreHrActor actor, CancellationToken cancellationToken);

    Task RecordAccessGrantedAsync(
        EmployeeDocument document,
        CoreHrActor actor,
        DateTimeOffset occurredAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task RecordAccessDeniedAsync(
        EmployeeDocument document,
        CoreHrActor actor,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}
