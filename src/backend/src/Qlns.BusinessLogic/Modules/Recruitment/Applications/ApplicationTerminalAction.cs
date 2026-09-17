namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

/// <summary>Terminal command of <c>POST /recruitment/applications/{applicationId}/{terminalAction}</c>.</summary>
public enum ApplicationTerminalAction
{
    Reject,
    Withdraw
}

public static class ApplicationTerminalActionNames
{
    public static string ToContract(this ApplicationTerminalAction action) => action switch
    {
        ApplicationTerminalAction.Reject => "reject",
        ApplicationTerminalAction.Withdraw => "withdraw",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    /// <summary>Terminal stage the action leads to.</summary>
    public static ApplicationStage ToStage(this ApplicationTerminalAction action) => action switch
    {
        ApplicationTerminalAction.Reject => ApplicationStage.Rejected,
        ApplicationTerminalAction.Withdraw => ApplicationStage.Withdrawn,
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string value, out ApplicationTerminalAction action)
    {
        action = value switch
        {
            "reject" => ApplicationTerminalAction.Reject,
            "withdraw" => ApplicationTerminalAction.Withdraw,
            _ => default
        };

        return value is "reject" or "withdraw";
    }
}
