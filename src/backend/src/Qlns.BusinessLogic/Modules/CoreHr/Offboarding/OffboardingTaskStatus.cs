namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>offboarding_tasks.status (ck_offboarding_task_status). Strictly pending → in_progress → completed.</summary>
public enum OffboardingTaskStatus
{
    Pending,
    InProgress,
    Completed
}

public static class OffboardingTaskStatusNames
{
    public static string ToContract(this OffboardingTaskStatus status) => status switch
    {
        OffboardingTaskStatus.Pending => "pending",
        OffboardingTaskStatus.InProgress => "in_progress",
        OffboardingTaskStatus.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out OffboardingTaskStatus status)
    {
        status = value switch
        {
            "pending" => OffboardingTaskStatus.Pending,
            "in_progress" => OffboardingTaskStatus.InProgress,
            "completed" => OffboardingTaskStatus.Completed,
            _ => default
        };

        return value is "pending" or "in_progress" or "completed";
    }
}
