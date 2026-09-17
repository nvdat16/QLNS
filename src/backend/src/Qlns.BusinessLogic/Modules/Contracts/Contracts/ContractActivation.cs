namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// Everything the repository must persist atomically when a contract is activated: the contract itself, the
/// employee's previous primary contract that gives way (expired by natural succession or terminated by an
/// approver's override, with the mandatory reason), and the probation review to open for probation contracts.
/// </summary>
public sealed record ContractActivation(
    Contract Contract,
    ContractStatus PreviousStatus,
    long ExpectedVersion,
    SupersededContract? Superseded,
    ProbationReviewDraft? ProbationReview);

/// <summary>The previous primary contract after its transition was applied in memory, with what is needed to save it.</summary>
public sealed record SupersededContract(
    Contract Contract,
    ContractStatus PreviousStatus,
    long ExpectedVersion,
    string? OverrideReason);
