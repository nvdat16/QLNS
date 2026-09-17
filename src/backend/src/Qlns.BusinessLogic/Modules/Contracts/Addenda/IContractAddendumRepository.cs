using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>
/// Persistence contract for contract addenda. Data scope is decided through the parent contract
/// (<see cref="Contracts.IContractRepository.GetByIdAsync"/>), so reads here are unscoped. Every write pairs the
/// business change with its audit rows in one transaction and returns false on a lost version race.
/// </summary>
public interface IContractAddendumRepository
{
    /// <summary>All addenda of the contract ordered by effective date, then id.</summary>
    Task<IReadOnlyList<ContractAddendum>> ListByContractAsync(long contractId, CancellationToken cancellationToken);

    Task<ContractAddendum?> GetByIdAsync(long addendumId, CancellationToken cancellationToken);

    Task<bool> AddendumNumberExistsAsync(string addendumNumber, CancellationToken cancellationToken);

    /// <summary>Inserts the draft plus audit row; throws <c>contracts.addendum.number_taken</c> on a unique violation.</summary>
    Task<ContractAddendum> InsertAsync(ContractAddendum addendum, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Submit, approve, mark-signed or cancel.</summary>
    Task<bool> SaveTransitionAsync(
        ContractAddendum addendum,
        ContractAddendumStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Make-effective: addendum, superseded addenda and the employee event in one transaction.</summary>
    Task<bool> SaveEffectiveAsync(AddendumActivation activation, CoreHrActor actor, CancellationToken cancellationToken);

    Task<bool> SaveSignedDocumentAsync(
        ContractAddendum addendum,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
