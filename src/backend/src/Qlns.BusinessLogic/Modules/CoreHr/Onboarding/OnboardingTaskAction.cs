namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>Transition requested through <c>POST /onboarding/tasks/{taskId}/{action}</c>.</summary>
public enum OnboardingTaskAction
{
    Start,
    Complete,
    Reopen
}

public static class OnboardingTaskActionNames
{
    public static string ToContract(this OnboardingTaskAction action) => action switch
    {
        OnboardingTaskAction.Start => "start",
        OnboardingTaskAction.Complete => "complete",
        OnboardingTaskAction.Reopen => "reopen",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string value, out OnboardingTaskAction action)
    {
        action = value switch
        {
            "start" => OnboardingTaskAction.Start,
            "complete" => OnboardingTaskAction.Complete,
            "reopen" => OnboardingTaskAction.Reopen,
            _ => default
        };

        return value is "start" or "complete" or "reopen";
    }
}
