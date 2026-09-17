namespace Qlns.BusinessLogic.Modules.CoreHr.Shared;

/// <summary>
/// Authenticated actor as established by the presentation layer. Business services never
/// read HTTP claims; they receive this value and decide permission, data scope and field policy.
/// </summary>
public sealed record CoreHrActor(
    long UserId,
    long? EmployeeId,
    CoreHrDataScope DataScope,
    IReadOnlySet<string> Permissions,
    string CorrelationId)
{
    public bool HasPermission(string permission) => Permissions.Contains(permission);

    public bool IsSelf(long employeeId) => EmployeeId == employeeId;

    /// <summary>
    /// True when the actor may see an employee that belongs to <paramref name="departmentId"/>:
    /// organization-wide scope, department scope containing that department, or the actor's own record.
    /// </summary>
    public bool CanAccessEmployee(long employeeId, long departmentId) =>
        IsSelf(employeeId) || DataScope.CoversDepartment(departmentId);

    public static CoreHrActor System(string correlationId) => new(
        UserId: 0,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Organization,
        Permissions: new HashSet<string>(),
        CorrelationId: correlationId);
}
