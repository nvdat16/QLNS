using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// EMP-07.2 checklist use cases: list the tasks of a visible case and move a task through
/// pending → in_progress → completed (reopen requires <see cref="OffboardingPermissions.Approve"/>).
/// A task is visible when the case's employee is in the actor's data scope or the task is assigned to the actor;
/// in both situations the actor still needs <see cref="OffboardingPermissions.Write"/> to change it.
/// </summary>
public sealed class OffboardingTaskService(
    IOffboardingTaskRepository tasks,
    IOffboardingCaseRepository cases,
    TimeProvider timeProvider)
{
    public const string ResourceName = "Offboarding task";
    public const string ReopenForbiddenCode = "corehr.offboarding.reopen_forbidden";
    public const string CaseNotActiveCode = "corehr.offboarding.case_not_active";
    private const string ConflictResource = "offboarding task";

    public async Task<IReadOnlyList<OffboardingTask>> ListAsync(
        long caseId,
        OffboardingTaskFilter filter,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(actor);

        var entry = await cases.GetByIdAsync(caseId, cancellationToken);
        if (entry is null || !actor.CanAccessEmployee(entry.Case.EmployeeId, entry.Employee.DepartmentId))
        {
            throw new CoreHrNotFoundException(OffboardingCaseService.ResourceName, caseId);
        }

        return await tasks.ListByCaseAsync(caseId, filter, cancellationToken);
    }

    public async Task<OffboardingTask> TransitionAsync(
        TransitionOffboardingTaskCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var entry = await tasks.GetByIdAsync(command.TaskId, cancellationToken);
        if (entry is null || !IsVisible(entry, actor))
        {
            throw new CoreHrNotFoundException(ResourceName, command.TaskId);
        }

        if (!actor.HasPermission(OffboardingPermissions.Write))
        {
            throw new CoreHrForbiddenException(
                OffboardingCaseService.WriteForbiddenCode,
                "Working on offboarding tasks requires the corehr.offboarding.write permission, also for assignees.");
        }

        var task = entry.Task;
        if (task.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        if (command.Action == OffboardingTaskAction.Reopen && !actor.HasPermission(OffboardingPermissions.Approve))
        {
            throw new CoreHrForbiddenException(
                ReopenForbiddenCode,
                "Only HR Manager (corehr.offboarding.approve) may reopen a completed offboarding task.");
        }

        if (entry.CaseStatus is not (OffboardingCaseStatus.Approved or OffboardingCaseStatus.InProgress))
        {
            throw new CoreHrBusinessRuleException(
                CaseNotActiveCode,
                $"Tasks can only be worked while the case is approved or in_progress; the case is {entry.CaseStatus.ToContract()}.")
            {
                Details = new Dictionary<string, object?> { ["caseStatus"] = entry.CaseStatus.ToContract() }
            };
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = task.Apply(command.Action, command.Reason, now);

        var saved = await tasks.SaveTransitionAsync(
            task,
            previousStatus,
            command.ExpectedVersion,
            command.Action == OffboardingTaskAction.Reopen ? command.Reason?.Trim() : null,
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return task;
    }

    private static bool IsVisible(OffboardingTaskEntry entry, CoreHrActor actor) =>
        actor.CanAccessEmployee(entry.EmployeeId, entry.EmployeeDepartmentId) ||
        entry.Task.AssignedToUserId == actor.UserId;
}
