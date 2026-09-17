namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>
/// Workflow state of a requisition (job_postings.status):
/// draft → pending_approval → approved → active_recruiting → closed; cancelled from any open state.
/// </summary>
public enum RequisitionStatus
{
    Draft,
    PendingApproval,
    Approved,
    ActiveRecruiting,
    Closed,
    Cancelled
}

public static class RequisitionStatusNames
{
    public static string ToContract(this RequisitionStatus status) => status switch
    {
        RequisitionStatus.Draft => "draft",
        RequisitionStatus.PendingApproval => "pending_approval",
        RequisitionStatus.Approved => "approved",
        RequisitionStatus.ActiveRecruiting => "active_recruiting",
        RequisitionStatus.Closed => "closed",
        RequisitionStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string value, out RequisitionStatus status)
    {
        status = value switch
        {
            "draft" => RequisitionStatus.Draft,
            "pending_approval" => RequisitionStatus.PendingApproval,
            "approved" => RequisitionStatus.Approved,
            "active_recruiting" => RequisitionStatus.ActiveRecruiting,
            "closed" => RequisitionStatus.Closed,
            "cancelled" => RequisitionStatus.Cancelled,
            _ => default
        };

        return value is "draft" or "pending_approval" or "approved" or "active_recruiting" or "closed" or "cancelled";
    }
}
