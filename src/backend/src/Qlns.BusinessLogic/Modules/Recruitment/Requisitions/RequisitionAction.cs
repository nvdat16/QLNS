namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>Transition requested through <c>POST /recruitment/requisitions/{requisitionId}/{action}</c>.</summary>
public enum RequisitionAction
{
    Submit,
    Approve,
    Reject,
    Publish,
    Close,
    Cancel
}

public static class RequisitionActionNames
{
    public static string ToContract(this RequisitionAction action) => action switch
    {
        RequisitionAction.Submit => "submit",
        RequisitionAction.Approve => "approve",
        RequisitionAction.Reject => "reject",
        RequisitionAction.Publish => "publish",
        RequisitionAction.Close => "close",
        RequisitionAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string value, out RequisitionAction action)
    {
        action = value switch
        {
            "submit" => RequisitionAction.Submit,
            "approve" => RequisitionAction.Approve,
            "reject" => RequisitionAction.Reject,
            "publish" => RequisitionAction.Publish,
            "close" => RequisitionAction.Close,
            "cancel" => RequisitionAction.Cancel,
            _ => default
        };

        return value is "submit" or "approve" or "reject" or "publish" or "close" or "cancel";
    }
}
