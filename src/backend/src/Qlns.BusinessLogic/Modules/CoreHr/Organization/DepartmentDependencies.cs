namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>
/// Records that block deleting a department. An open requisition is a job posting whose status is
/// one of <c>draft</c>, <c>pending_approval</c>, <c>approved</c>, <c>active_recruiting</c>.
/// </summary>
public sealed record DepartmentDependencies(int ChildDepartments, int Employees, int OpenRequisitions)
{
    public static DepartmentDependencies None { get; } = new(0, 0, 0);

    public bool Any => ChildDepartments > 0 || Employees > 0 || OpenRequisitions > 0;
}
