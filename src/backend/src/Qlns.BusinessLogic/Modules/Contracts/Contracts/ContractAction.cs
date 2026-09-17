namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>Transition requested through <c>POST /contracts/{contractId}/{action}</c>.</summary>
public enum ContractAction
{
    Approve,
    Activate,
    Terminate,
    Cancel
}

public static class ContractActionNames
{
    public static string ToContract(this ContractAction action) => action switch
    {
        ContractAction.Approve => "approve",
        ContractAction.Activate => "activate",
        ContractAction.Terminate => "terminate",
        ContractAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out ContractAction action)
    {
        action = value switch
        {
            "approve" => ContractAction.Approve,
            "activate" => ContractAction.Activate,
            "terminate" => ContractAction.Terminate,
            "cancel" => ContractAction.Cancel,
            _ => default
        };

        return value is "approve" or "activate" or "terminate" or "cancel";
    }
}
