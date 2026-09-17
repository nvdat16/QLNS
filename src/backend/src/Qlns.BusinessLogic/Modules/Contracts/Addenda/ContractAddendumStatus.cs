namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>
/// contract_addenda.status (ck_contract_addendum_status). Workflow: draft → pending_approval → approved → effective;
/// an effective addendum becomes superseded when a later one changes the same terms; draft, pending_approval and
/// approved may be cancelled.
/// </summary>
public enum ContractAddendumStatus
{
    Draft,
    PendingApproval,
    Approved,
    Effective,
    Superseded,
    Cancelled
}

public static class ContractAddendumStatusNames
{
    public static string ToContract(this ContractAddendumStatus status) => status switch
    {
        ContractAddendumStatus.Draft => "draft",
        ContractAddendumStatus.PendingApproval => "pending_approval",
        ContractAddendumStatus.Approved => "approved",
        ContractAddendumStatus.Effective => "effective",
        ContractAddendumStatus.Superseded => "superseded",
        ContractAddendumStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out ContractAddendumStatus status)
    {
        status = value switch
        {
            "draft" => ContractAddendumStatus.Draft,
            "pending_approval" => ContractAddendumStatus.PendingApproval,
            "approved" => ContractAddendumStatus.Approved,
            "effective" => ContractAddendumStatus.Effective,
            "superseded" => ContractAddendumStatus.Superseded,
            "cancelled" => ContractAddendumStatus.Cancelled,
            _ => default
        };

        return value is "draft" or "pending_approval" or "approved" or "effective" or "superseded" or "cancelled";
    }
}
