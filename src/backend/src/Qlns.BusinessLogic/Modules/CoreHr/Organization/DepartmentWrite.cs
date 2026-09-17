namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>Create/replace payload for a department (OpenAPI <c>DepartmentWrite</c>).</summary>
public sealed record DepartmentWrite(
    string Code,
    string Name,
    long? ParentDepartmentId,
    string? CostCenter,
    string? Description);
