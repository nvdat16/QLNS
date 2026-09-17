namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>Employment status (employees.status). Contract values are snake_case strings.</summary>
public enum EmployeeStatus
{
    Probation,
    Active,
    Suspended,
    Terminated
}

public static class EmployeeStatusNames
{
    public static string ToContract(this EmployeeStatus status) => status switch
    {
        EmployeeStatus.Probation => "probation",
        EmployeeStatus.Active => "active",
        EmployeeStatus.Suspended => "suspended",
        EmployeeStatus.Terminated => "terminated",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out EmployeeStatus status)
    {
        status = value switch
        {
            "probation" => EmployeeStatus.Probation,
            "active" => EmployeeStatus.Active,
            "suspended" => EmployeeStatus.Suspended,
            "terminated" => EmployeeStatus.Terminated,
            _ => default
        };

        return value is "probation" or "active" or "suspended" or "terminated";
    }
}
