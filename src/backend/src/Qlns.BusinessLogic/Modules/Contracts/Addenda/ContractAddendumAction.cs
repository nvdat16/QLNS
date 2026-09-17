namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>Transition requested through <c>POST /contract-addenda/{addendumId}/{action}</c>.</summary>
public enum ContractAddendumAction
{
    Submit,
    Approve,
    MarkSigned,
    MakeEffective,
    Cancel
}

public static class ContractAddendumActionNames
{
    public static string ToContract(this ContractAddendumAction action) => action switch
    {
        ContractAddendumAction.Submit => "submit",
        ContractAddendumAction.Approve => "approve",
        ContractAddendumAction.MarkSigned => "mark-signed",
        ContractAddendumAction.MakeEffective => "make-effective",
        ContractAddendumAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out ContractAddendumAction action)
    {
        action = value switch
        {
            "submit" => ContractAddendumAction.Submit,
            "approve" => ContractAddendumAction.Approve,
            "mark-signed" => ContractAddendumAction.MarkSigned,
            "make-effective" => ContractAddendumAction.MakeEffective,
            "cancel" => ContractAddendumAction.Cancel,
            _ => default
        };

        return value is "submit" or "approve" or "mark-signed" or "make-effective" or "cancel";
    }
}
