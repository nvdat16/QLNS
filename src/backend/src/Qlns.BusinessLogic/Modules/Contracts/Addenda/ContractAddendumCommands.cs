using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

public sealed record CreateContractAddendumCommand(
    long ContractId,
    ContractAddendumWrite Write,
    CoreHrActor Actor);

public sealed record TransitionContractAddendumCommand(
    long AddendumId,
    ContractAddendumAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);

public sealed record UploadSignedAddendumCommand(
    long AddendumId,
    long ExpectedVersion,
    SignedDocumentUpload Upload,
    CoreHrActor Actor);

/// <summary>
/// Everything the repository persists atomically when an addendum becomes effective: the addendum, the older
/// effective addenda it supersedes and the employee event for master-data changes (null when none).
/// </summary>
public sealed record AddendumActivation(
    ContractAddendum Addendum,
    long ExpectedVersion,
    IReadOnlyList<SupersededAddendum> Superseded,
    MasterDataEventDraft? EmployeeEvent);

public sealed record SupersededAddendum(ContractAddendum Addendum, long ExpectedVersion);
