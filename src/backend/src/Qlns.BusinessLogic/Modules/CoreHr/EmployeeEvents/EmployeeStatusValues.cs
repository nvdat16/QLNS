namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>
/// employees.status contract values (ck_employee_status). Kept local so this feature does not depend on
/// the Employees feature; the values are the same snake_case strings stored in the database.
/// </summary>
public static class EmployeeStatusValues
{
    public const string Probation = "probation";
    public const string Active = "active";
    public const string Suspended = "suspended";
    public const string Terminated = "terminated";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Probation, Active, Suspended, Terminated
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
