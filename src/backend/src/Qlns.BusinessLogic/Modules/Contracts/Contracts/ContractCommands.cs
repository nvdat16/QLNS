using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

public sealed record CreateContractCommand(ContractWrite Write, CoreHrActor Actor);

public sealed record ReplaceContractCommand(
    long ContractId,
    long ExpectedVersion,
    ContractWrite Write,
    CoreHrActor Actor);

public sealed record TransitionContractCommand(
    long ContractId,
    ContractAction Action,
    long ExpectedVersion,
    ContractActionOptions Options,
    CoreHrActor Actor);

public sealed record UploadSignedContractCommand(
    long ContractId,
    long ExpectedVersion,
    SignedDocumentUpload Upload,
    CoreHrActor Actor);

/// <summary>Filters of <c>GET /contracts</c>. Data scope comes from the actor, never from the query.</summary>
public sealed record ContractSearchQuery(
    long? EmployeeId,
    ContractType? Type,
    ContractStatus? Status,
    PageRequest Page);

/// <summary>Parameters of <c>GET /contracts/expiring</c>; a null <see cref="AsOf"/> means today.</summary>
public sealed record ExpiringContractsQuery(
    DateOnly? AsOf,
    int WithinDays,
    PageRequest Page);
