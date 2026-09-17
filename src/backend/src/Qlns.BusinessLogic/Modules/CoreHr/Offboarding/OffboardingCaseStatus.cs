namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>offboarding_cases.status (ck_offboarding_status). Contract values are snake_case.</summary>
public enum OffboardingCaseStatus
{
    Draft,
    PendingApproval,
    Approved,
    InProgress,
    Completed,
    Cancelled
}

public static class OffboardingCaseStatusNames
{
    public static string ToContract(this OffboardingCaseStatus status) => status switch
    {
        OffboardingCaseStatus.Draft => "draft",
        OffboardingCaseStatus.PendingApproval => "pending_approval",
        OffboardingCaseStatus.Approved => "approved",
        OffboardingCaseStatus.InProgress => "in_progress",
        OffboardingCaseStatus.Completed => "completed",
        OffboardingCaseStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out OffboardingCaseStatus status)
    {
        status = value switch
        {
            "draft" => OffboardingCaseStatus.Draft,
            "pending_approval" => OffboardingCaseStatus.PendingApproval,
            "approved" => OffboardingCaseStatus.Approved,
            "in_progress" => OffboardingCaseStatus.InProgress,
            "completed" => OffboardingCaseStatus.Completed,
            "cancelled" => OffboardingCaseStatus.Cancelled,
            _ => default
        };

        return value is "draft" or "pending_approval" or "approved" or "in_progress" or "completed" or "cancelled";
    }

    /// <summary>Statuses covered by the partial unique index ux_offboarding_open_case.</summary>
    public static bool IsOpen(this OffboardingCaseStatus status) =>
        status is OffboardingCaseStatus.Draft or OffboardingCaseStatus.PendingApproval or
            OffboardingCaseStatus.Approved or OffboardingCaseStatus.InProgress;
}
