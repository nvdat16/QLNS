namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>Permission claim values (claim type <c>permission</c>) of the Recruitment → Requisitions feature (REC-01).</summary>
public static class RequisitionPermissions
{
    public const string Read = "recruitment.requisition.read";
    public const string Write = "recruitment.requisition.write";
    public const string Approve = "recruitment.requisition.approve";
    public const string Publish = "recruitment.requisition.publish";
}
