using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// Persistence contract for employment contracts. Reads apply the actor's data scope in SQL (organization-wide,
/// the contract employee's department, or the actor's own employee record); contracts outside scope are absent.
/// Every write pairs the business change with its audit (and outbox) rows in one transaction and returns false
/// when the conditional update on <c>id AND version = expected</c> matched no row.
/// </summary>
public interface IContractRepository
{
    /// <summary>Employee with department and manager's user id, or null when the employee does not exist.</summary>
    Task<ContractEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken);

    Task<PagedResult<Contract>> SearchAsync(ContractSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken);

    Task<Contract?> GetByIdAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken);

    Task<bool> ContractNumberExistsAsync(string contractNumber, long? excludeContractId, CancellationToken cancellationToken);

    /// <summary>The employee's other primary contract in status executed or active, if any.</summary>
    Task<Contract?> FindOtherPrimaryInForceAsync(long employeeId, long excludeContractId, CancellationToken cancellationToken);

    /// <summary>
    /// In-force contracts within scope whose end date lies in [<paramref name="asOf"/>, window end] where the window
    /// end is the per-type bound of <see cref="ExpiryAlertPolicy.Windows"/>. Ordered by end date, then id.
    /// </summary>
    Task<PagedResult<Contract>> SearchExpiringAsync(
        DateOnly asOf,
        IReadOnlyList<ExpiryAlertWindow> windows,
        CoreHrActor actor,
        PageRequest page,
        CancellationToken cancellationToken);

    /// <summary>Active contracts whose end date lies strictly before <paramref name="today"/>, ordered by end date, id.</summary>
    Task<IReadOnlyList<Contract>> ListDueForExpiryAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Inserts the draft plus audit row; throws <c>contracts.contract.number_taken</c> on a unique violation.</summary>
    Task<Contract> InsertAsync(Contract contract, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Writes all editable columns of a draft; throws <c>contracts.contract.number_taken</c> on a unique violation.</summary>
    Task<bool> SaveReplacementAsync(
        Contract contract,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Approve (with outbox notification), terminate, cancel or expire.</summary>
    Task<bool> SaveTransitionAsync(
        Contract contract,
        ContractStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Activation with the superseded primary contract and the probation review in one transaction.</summary>
    Task<bool> SaveActivationAsync(ContractActivation activation, CoreHrActor actor, CancellationToken cancellationToken);

    Task<bool> SaveSignedDocumentAsync(
        Contract contract,
        ContractStatus previousStatus,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Audit row for an issued download link; never contains the object key.</summary>
    Task RecordDownloadAsync(Contract contract, CoreHrActor actor, DateTimeOffset occurredAt, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
