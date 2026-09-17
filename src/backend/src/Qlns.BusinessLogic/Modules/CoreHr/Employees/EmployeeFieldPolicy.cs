using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>Which fields of an <see cref="Employee"/> the actor may see.</summary>
public enum EmployeeFieldVisibility
{
    /// <summary>Public work fields only: personal contact, birth date, gender and addresses are hidden.</summary>
    Public,

    /// <summary>All fields, including personal and restricted ones.</summary>
    Full
}

/// <summary>
/// Field policy for EMP-01.1 scenario 2: a colleague sees only public work information; the employee
/// themselves and holders of <see cref="CoreHrPermissions.EmployeeReadSensitive"/> see everything.
/// </summary>
public static class EmployeeFieldPolicy
{
    public static EmployeeFieldVisibility Resolve(CoreHrActor actor, Employee employee)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(employee);

        return actor.IsSelf(employee.Id) || actor.HasPermission(CoreHrPermissions.EmployeeReadSensitive)
            ? EmployeeFieldVisibility.Full
            : EmployeeFieldVisibility.Public;
    }
}
