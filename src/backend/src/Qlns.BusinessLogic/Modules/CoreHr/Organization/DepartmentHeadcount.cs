namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>Number of employees assigned to a department whose status is not <c>terminated</c>.</summary>
public sealed record DepartmentHeadcount(long DepartmentId, int Headcount);
