namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>A department together with its computed headcount (OpenAPI <c>Department</c>).</summary>
public sealed record DepartmentDetail(Department Department, int Headcount);
