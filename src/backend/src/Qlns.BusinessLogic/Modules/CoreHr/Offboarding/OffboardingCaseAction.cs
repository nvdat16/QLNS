namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>Transition requested through <c>POST /offboarding/cases/{caseId}/{action}</c>.</summary>
public enum OffboardingCaseAction
{
    Approve,
    Start,
    Complete,
    Cancel
}

public static class OffboardingCaseActionNames
{
    public static string ToContract(this OffboardingCaseAction action) => action switch
    {
        OffboardingCaseAction.Approve => "approve",
        OffboardingCaseAction.Start => "start",
        OffboardingCaseAction.Complete => "complete",
        OffboardingCaseAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out OffboardingCaseAction action)
    {
        action = value switch
        {
            "approve" => OffboardingCaseAction.Approve,
            "start" => OffboardingCaseAction.Start,
            "complete" => OffboardingCaseAction.Complete,
            "cancel" => OffboardingCaseAction.Cancel,
            _ => default
        };

        return value is "approve" or "start" or "complete" or "cancel";
    }
}
