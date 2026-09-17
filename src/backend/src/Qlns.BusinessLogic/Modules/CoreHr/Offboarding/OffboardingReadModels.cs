namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Snapshot of the <c>employees</c> columns offboarding needs: data scope (department), eligibility (status)
/// and the line manager's user account for <c>manager.*</c> checklist tasks.
/// </summary>
public sealed record OffboardingEmployee(
    long Id,
    long DepartmentId,
    string Status,
    long? ManagerUserId);

/// <summary>
/// Repository read model of one case: the aggregate plus the employee snapshot (scope, status), the count of
/// outstanding blocking tasks and the notice period of the employee's primary executed/active contract.
/// </summary>
public sealed record OffboardingCaseEntry(
    OffboardingCase Case,
    OffboardingEmployee Employee,
    int BlockingTasksOutstanding,
    int? NoticePeriodDays);

/// <summary>Service result: the case with the derived contract fields of OpenAPI OffboardingCase.</summary>
public sealed record OffboardingCaseView(
    OffboardingCase Case,
    int BlockingTasksOutstanding,
    int? NoticePeriodShortfallDays)
{
    public static OffboardingCaseView From(OffboardingCaseEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new OffboardingCaseView(
            entry.Case,
            entry.BlockingTasksOutstanding,
            entry.Case.NoticePeriodShortfallDays(entry.NoticePeriodDays));
    }
}

/// <summary>Repository read model of one task with what is needed to decide visibility.</summary>
public sealed record OffboardingTaskEntry(
    OffboardingTask Task,
    long EmployeeId,
    long EmployeeDepartmentId,
    OffboardingCaseStatus CaseStatus);
