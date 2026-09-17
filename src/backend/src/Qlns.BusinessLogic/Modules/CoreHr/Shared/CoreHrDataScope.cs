namespace Qlns.BusinessLogic.Modules.CoreHr.Shared;

/// <summary>
/// Server-side data scope (user_roles.data_scope_type). <c>self</c> is represented by an empty
/// department set without organization-wide access; the actor's own employee id grants self access.
/// </summary>
public sealed record CoreHrDataScope(bool OrganizationWide, IReadOnlySet<long> DepartmentIds)
{
    public static CoreHrDataScope Organization { get; } = new(true, new HashSet<long>());

    public static CoreHrDataScope Self { get; } = new(false, new HashSet<long>());

    public static CoreHrDataScope Departments(params long[] departmentIds) =>
        new(false, departmentIds.ToHashSet());

    public bool IsSelfOnly => !OrganizationWide && DepartmentIds.Count == 0;

    public bool CoversDepartment(long departmentId) =>
        OrganizationWide || DepartmentIds.Contains(departmentId);
}
