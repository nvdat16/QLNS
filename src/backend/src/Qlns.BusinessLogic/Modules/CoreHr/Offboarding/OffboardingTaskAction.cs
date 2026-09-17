namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>Transition requested through <c>POST /offboarding/tasks/{taskId}/{action}</c>.</summary>
public enum OffboardingTaskAction
{
    Start,
    Complete,
    Reopen
}

public static class OffboardingTaskActionNames
{
    public static string ToContract(this OffboardingTaskAction action) => action switch
    {
        OffboardingTaskAction.Start => "start",
        OffboardingTaskAction.Complete => "complete",
        OffboardingTaskAction.Reopen => "reopen",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out OffboardingTaskAction action)
    {
        action = value switch
        {
            "start" => OffboardingTaskAction.Start,
            "complete" => OffboardingTaskAction.Complete,
            "reopen" => OffboardingTaskAction.Reopen,
            _ => default
        };

        return value is "start" or "complete" or "reopen";
    }
}
