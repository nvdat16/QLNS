namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>employee_events.status (ck_employee_event_status). Contract values are snake_case.</summary>
public enum EmployeeEventStatus
{
    Draft,
    PendingApproval,
    Approved,
    Applied,
    Cancelled
}

public static class EmployeeEventStatusNames
{
    public static string ToContract(this EmployeeEventStatus status) => status switch
    {
        EmployeeEventStatus.Draft => "draft",
        EmployeeEventStatus.PendingApproval => "pending_approval",
        EmployeeEventStatus.Approved => "approved",
        EmployeeEventStatus.Applied => "applied",
        EmployeeEventStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out EmployeeEventStatus status)
    {
        status = value switch
        {
            "draft" => EmployeeEventStatus.Draft,
            "pending_approval" => EmployeeEventStatus.PendingApproval,
            "approved" => EmployeeEventStatus.Approved,
            "applied" => EmployeeEventStatus.Applied,
            "cancelled" => EmployeeEventStatus.Cancelled,
            _ => default
        };

        return value is "draft" or "pending_approval" or "approved" or "applied" or "cancelled";
    }
}
