namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>Workflow state of an onboarding task (onboarding_tasks.status). Strictly pending → in_progress → completed.</summary>
public enum OnboardingTaskStatus
{
    Pending,
    InProgress,
    Completed
}

public static class OnboardingTaskStatusNames
{
    public static string ToContract(this OnboardingTaskStatus status) => status switch
    {
        OnboardingTaskStatus.Pending => "pending",
        OnboardingTaskStatus.InProgress => "in_progress",
        OnboardingTaskStatus.Completed => "completed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string value, out OnboardingTaskStatus status)
    {
        status = value switch
        {
            "pending" => OnboardingTaskStatus.Pending,
            "in_progress" => OnboardingTaskStatus.InProgress,
            "completed" => OnboardingTaskStatus.Completed,
            _ => default
        };

        return value is "pending" or "in_progress" or "completed";
    }
}
