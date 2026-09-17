using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>
/// Use cases of EMP-03.1: search the onboarding checklist (with overdue flag), move a task through
/// its workflow and update its assignment. Data scope is enforced by the repository; permission
/// for reopening is enforced here.
/// </summary>
public sealed class OnboardingTaskService(
    IOnboardingTaskRepository repository,
    TimeProvider timeProvider)
{
    private const string ResourceName = "Onboarding task";
    private const string ConflictResource = "onboarding task";

    public async Task<PagedResult<OnboardingTaskView>> SearchAsync(
        OnboardingTaskSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);

        var now = timeProvider.GetUtcNow();
        var result = await repository.SearchAsync(query, actor, now, cancellationToken);
        return result.Map(task => new OnboardingTaskView(task, task.IsOverdue(now)));
    }

    public async Task<OnboardingTaskView> TransitionAsync(
        TransitionOnboardingTaskCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await LoadAsync(command.TaskId, command.ExpectedVersion, command.Actor, cancellationToken);

        if (command.Action == OnboardingTaskAction.Reopen &&
            !command.Actor.HasPermission(CoreHrPermissions.OnboardingReopen))
        {
            throw new CoreHrForbiddenException(
                "corehr.onboarding.reopen_forbidden",
                "Only HR Officer/HR Manager may reopen a completed task.");
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = task.Apply(command.Action, command.Reason, now);

        var saved = await repository.SaveTransitionAsync(
            task,
            previousStatus,
            command.ExpectedVersion,
            command.Action == OnboardingTaskAction.Reopen ? command.Reason?.Trim() : null,
            command.Actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return new OnboardingTaskView(task, task.IsOverdue(now));
    }

    public async Task<OnboardingTaskView> UpdateAssignmentAsync(
        UpdateOnboardingTaskCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var task = await LoadAsync(command.TaskId, command.ExpectedVersion, command.Actor, cancellationToken);

        if (command.Write.AssignedToUserId is > 0 &&
            !await repository.UserExistsAsync(command.Write.AssignedToUserId.Value, cancellationToken))
        {
            throw CoreHrValidationException.For("assignedToUserId", "Assigned user does not exist or is not active.");
        }

        var now = timeProvider.GetUtcNow();
        var changedFields = task.UpdateAssignment(command.Write, now);

        var saved = await repository.SaveAssignmentAsync(
            task,
            command.ExpectedVersion,
            changedFields,
            command.Actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return new OnboardingTaskView(task, task.IsOverdue(now));
    }

    private async Task<OnboardingTask> LoadAsync(
        long taskId,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(taskId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, taskId);

        if (task.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return task;
    }
}
