using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.Contracts.Shared;

namespace Qlns.Api.Modules.Contracts.Shared;

/// <summary>
/// Endpoint-level authorization policies of the Contracts module. Each policy requires the matching
/// <c>permission</c> claim; data scope and action-level permissions (approve, overlap override) are enforced
/// afterwards by the business services. The write policy also admits approvers so that a single transition
/// endpoint can serve approve as well as activate/terminate/cancel.
/// </summary>
public static class ContractPolicies
{
    public const string Read = "ContractsRead";
    public const string Write = "ContractsWrite";
    public const string Approve = "ContractsApprove";

    public static void AddContractPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(Read, p => p.RequireClaim("permission", ContractPermissions.Read));
        options.AddPolicy(Write, p => p.RequireClaim("permission", ContractPermissions.Write, ContractPermissions.Approve));
        options.AddPolicy(Approve, p => p.RequireClaim("permission", ContractPermissions.Approve));
    }
}
