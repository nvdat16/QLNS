namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>Workflow actions of <c>POST /employee-events/{eventId}/{action}</c>.</summary>
public enum EmployeeEventAction
{
    Submit,
    Approve,
    Cancel
}

public static class EmployeeEventActionNames
{
    public static string ToContract(this EmployeeEventAction action) => action switch
    {
        EmployeeEventAction.Submit => "submit",
        EmployeeEventAction.Approve => "approve",
        EmployeeEventAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out EmployeeEventAction action)
    {
        action = value switch
        {
            "submit" => EmployeeEventAction.Submit,
            "approve" => EmployeeEventAction.Approve,
            "cancel" => EmployeeEventAction.Cancel,
            _ => default
        };

        return value is "submit" or "approve" or "cancel";
    }
}
