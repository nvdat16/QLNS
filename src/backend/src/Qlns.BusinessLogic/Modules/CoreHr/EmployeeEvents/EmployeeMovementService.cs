using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>
/// EMP-04.1 use cases: list movement history, create a draft proposal, run the submit/approve/cancel
/// workflow and apply due approved events to master data (Effective-Date Worker).
/// Data scope is enforced here: an employee outside the actor's scope is reported as not found.
/// </summary>
public sealed class EmployeeMovementService(
    IEmployeeEventRepository repository,
    TimeProvider timeProvider)
{
    public async Task<PagedResult<EmployeeEvent>> ListAsync(
        long employeeId,
        PageRequest page,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await GetVisibleEmployeeAsync(employeeId, actor, cancellationToken);
        return await repository.ListByEmployeeAsync(employeeId, page, cancellationToken);
    }

    public async Task<EmployeeEvent> CreateAsync(CreateEmployeeEventCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        await GetVisibleEmployeeAsync(command.EmployeeId, actor, cancellationToken);

        if (!actor.HasPermission(CoreHrPermissions.EventWrite))
        {
            throw new CoreHrForbiddenException(
                "corehr.event.write_forbidden",
                "Creating employee events requires the corehr.event.write permission.");
        }

        var draft = EmployeeEvent.CreateDraft(command.EmployeeId, command.Write, actor.UserId, timeProvider.GetUtcNow());

        if (draft.CompensatesEventId is { } compensatesEventId)
        {
            var compensated = await repository.GetByIdAsync(compensatesEventId, cancellationToken);
            if (compensated is null ||
                compensated.EmployeeId != command.EmployeeId ||
                compensated.Status != EmployeeEventStatus.Applied)
            {
                throw CoreHrValidationException.For(
                    "compensatesEventId",
                    "compensatesEventId must reference an applied event of the same employee.");
            }
        }

        var conflicts = await repository.FindConflictingAsync(
            command.EmployeeId,
            draft.EffectiveDate,
            draft.ChangedFields,
            excludeEventId: null,
            cancellationToken);

        if (conflicts.Count > 0)
        {
            var conflict = conflicts[0];
            throw new CoreHrBusinessRuleException(
                "corehr.event.conflicting_field",
                $"Event {conflict.EventId} already changes {string.Join(", ", conflict.Fields)} on {draft.EffectiveDate:yyyy-MM-dd}.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["conflictingEventId"] = conflict.EventId,
                    ["fields"] = conflict.Fields
                }
            };
        }

        return await repository.InsertAsync(draft, actor, cancellationToken);
    }

    public async Task<EmployeeEvent> TransitionAsync(
        TransitionEmployeeEventCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var employeeEvent = await repository.GetByIdAsync(command.EventId, cancellationToken)
            ?? throw new CoreHrNotFoundException("Employee event", command.EventId);

        var employee = await repository.GetEmployeeMasterDataAsync(employeeEvent.EmployeeId, cancellationToken);
        if (employee is null || !actor.CanAccessEmployee(employee.EmployeeId, employee.DepartmentId))
        {
            throw new CoreHrNotFoundException("Employee event", command.EventId);
        }

        if (employeeEvent.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException("employee event");
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = employeeEvent.Status;

        switch (command.Action)
        {
            case EmployeeEventAction.Approve:
                RequireApprove(actor);
                employeeEvent.Approve(actor.UserId, now);
                break;
            case EmployeeEventAction.Submit:
                RequireWriteOrCreator(actor, employeeEvent);
                employeeEvent.Submit(now);
                break;
            case EmployeeEventAction.Cancel:
                RequireWriteOrCreator(actor, employeeEvent);
                employeeEvent.Cancel(command.Reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unknown employee event action.");
        }

        var saved = await repository.SaveTransitionAsync(
            employeeEvent,
            previousStatus,
            command.ExpectedVersion,
            command.Reason,
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException("employee event");
        }

        return employeeEvent;
    }

    /// <summary>
    /// Effective-Date Worker entry point (sequence diagram §5): applies every approved event due on or before
    /// <paramref name="today"/>. Events that lose a version race or violate an employee constraint are reported
    /// as conflicted for reconciliation instead of aborting the run.
    /// </summary>
    public async Task<ApplyDueEventsResult> ApplyDueEventsAsync(
        DateOnly today,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var dueEvents = await repository.ListDueApprovedAsync(today, cancellationToken);
        var applied = 0;
        var conflicted = new List<long>();

        foreach (var dueEvent in dueEvents)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var employee = await repository.GetEmployeeMasterDataAsync(dueEvent.EmployeeId, cancellationToken);
            if (employee is null)
            {
                conflicted.Add(dueEvent.Id);
                continue;
            }

            var expectedEventVersion = dueEvent.Version;
            bool saved;
            try
            {
                var newData = dueEvent.ApplyTo(employee, today, timeProvider.GetUtcNow());
                saved = await repository.SaveAppliedAsync(
                    dueEvent,
                    expectedEventVersion,
                    newData,
                    employee.Version,
                    actor,
                    cancellationToken);
            }
            catch (CoreHrBusinessRuleException)
            {
                saved = false;
            }

            if (saved)
            {
                applied++;
            }
            else
            {
                conflicted.Add(dueEvent.Id);
            }
        }

        return new ApplyDueEventsResult(applied, conflicted);
    }

    private async Task<EmployeeMasterData> GetVisibleEmployeeAsync(
        long employeeId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var employee = await repository.GetEmployeeMasterDataAsync(employeeId, cancellationToken);
        if (employee is null || !actor.CanAccessEmployee(employeeId, employee.DepartmentId))
        {
            throw new CoreHrNotFoundException("Employee", employeeId);
        }

        return employee;
    }

    private static void RequireApprove(CoreHrActor actor)
    {
        if (!actor.HasPermission(CoreHrPermissions.EventApprove))
        {
            throw new CoreHrForbiddenException(
                "corehr.event.approve_forbidden",
                "Approving employee events requires the corehr.event.approve permission.");
        }
    }

    private static void RequireWriteOrCreator(CoreHrActor actor, EmployeeEvent employeeEvent)
    {
        if (!actor.HasPermission(CoreHrPermissions.EventWrite) && actor.UserId != employeeEvent.CreatedBy)
        {
            throw new CoreHrForbiddenException(
                "corehr.event.write_forbidden",
                "Submitting or cancelling an employee event requires the corehr.event.write permission or being its creator.");
        }
    }
}
