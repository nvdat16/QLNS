namespace Qlns.BusinessLogic.Modules.Contracts.Shared;

/// <summary>
/// Permission claim values of the Contracts module (claim type <c>permission</c>). Endpoint policies in
/// Qlns.Api require the coarse claim; the services re-check the finer approve-level claim themselves.
/// Addenda share the contract permissions: whoever may write or approve contracts may do so for their addenda.
/// </summary>
public static class ContractPermissions
{
    public const string Read = "contracts.contract.read";
    public const string Write = "contracts.contract.write";
    public const string Approve = "contracts.contract.approve";
}
