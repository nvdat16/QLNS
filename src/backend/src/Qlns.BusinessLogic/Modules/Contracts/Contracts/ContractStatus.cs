namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// contracts.status (ck_contract_status). Workflow: draft → approved → (executed) → active → expired | terminated;
/// draft and approved may be cancelled. <c>executed</c> means signed but not yet in force.
/// </summary>
public enum ContractStatus
{
    Draft,
    Approved,
    Executed,
    Active,
    Expired,
    Terminated,
    Cancelled
}

public static class ContractStatusNames
{
    /// <summary>Statuses in which a primary contract occupies the employee's single in-force slot (ux_contracts_primary_active).</summary>
    public static bool IsInForce(this ContractStatus status) => status is ContractStatus.Executed or ContractStatus.Active;

    public static string ToContract(this ContractStatus status) => status switch
    {
        ContractStatus.Draft => "draft",
        ContractStatus.Approved => "approved",
        ContractStatus.Executed => "executed",
        ContractStatus.Active => "active",
        ContractStatus.Expired => "expired",
        ContractStatus.Terminated => "terminated",
        ContractStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out ContractStatus status)
    {
        status = value switch
        {
            "draft" => ContractStatus.Draft,
            "approved" => ContractStatus.Approved,
            "executed" => ContractStatus.Executed,
            "active" => ContractStatus.Active,
            "expired" => ContractStatus.Expired,
            "terminated" => ContractStatus.Terminated,
            "cancelled" => ContractStatus.Cancelled,
            _ => default
        };

        return value is "draft" or "approved" or "executed" or "active" or "expired" or "terminated" or "cancelled";
    }
}
