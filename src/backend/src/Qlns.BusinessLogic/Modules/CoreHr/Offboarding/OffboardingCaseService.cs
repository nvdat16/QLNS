using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// EMP-07.1 / EMP-07.2 case use cases: search, open, read and run the approve → start → complete / cancel workflow.
/// Data scope is enforced here (employee outside scope ⇒ 404); action-level permissions
/// (<see cref="OffboardingPermissions.Write"/>, <see cref="OffboardingPermissions.Approve"/>) are re-checked here
/// because the endpoint policy only requires the coarse claim.
/// </summary>
public sealed class OffboardingCaseService(
    IOffboardingCaseRepository repository,
    TimeProvider timeProvider)
{
    public const string ResourceName = "Offboarding case";
    public const string WriteForbiddenCode = "corehr.offboarding.write_forbidden";
    public const string ApproveForbiddenCode = "corehr.offboarding.approve_forbidden";
    private const string ConflictResource = "offboarding case";

    public async Task<PagedResult<OffboardingCaseView>> SearchAsync(
        OffboardingCaseSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);

        var result = await repository.SearchAsync(query, actor, cancellationToken);
        return result.Map(OffboardingCaseView.From);
    }

    public async Task<OffboardingCaseView> GetAsync(long caseId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var entry = await LoadVisibleAsync(caseId, actor, cancellationToken);
        return OffboardingCaseView.From(entry);
    }

    public async Task<OffboardingCaseView> CreateAsync(CreateOffboardingCaseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;
        var write = command.Write;

        var employee = await repository.GetEmployeeAsync(write.EmployeeId, cancellationToken);
        if (employee is null || !actor.CanAccessEmployee(employee.Id, employee.DepartmentId))
        {
            throw new CoreHrNotFoundException("Employee", write.EmployeeId);
        }

        RequireWrite(actor);

        var now = timeProvider.GetUtcNow();
        var draft = OffboardingCase.Open(write, actor.UserId, Today(now), now);

        if (!OffboardingCase.IsEligibleEmployeeStatus(employee.Status))
        {
            throw new CoreHrBusinessRuleException(
                OffboardingCase.EmployeeNotEligibleCode,
                $"Only active or probation employees can be offboarded; employee {employee.Id} is {employee.Status}.")
            {
                Details = new Dictionary<string, object?> { ["employeeStatus"] = employee.Status }
            };
        }

        if (await repository.HasOpenCaseAsync(employee.Id, cancellationToken))
        {
            throw OpenCaseExists(employee.Id);
        }

        if (draft.HandoverToEmployeeId is { } handoverId)
        {
            var handover = await repository.GetEmployeeAsync(handoverId, cancellationToken);
            if (handover is null || handover.Status != EmployeeStatusValues.Active)
            {
                throw CoreHrValidationException.For(
                    "handoverToEmployeeId",
                    "The handover employee must exist and be active.");
            }
        }

        var noticePeriodDays = await repository.GetNoticePeriodDaysAsync(employee.Id, cancellationToken);
        var shortfall = draft.NoticePeriodShortfallDays(noticePeriodDays);

        var persisted = await repository.InsertAsync(draft, shortfall, actor, cancellationToken);
        return new OffboardingCaseView(persisted, BlockingTasksOutstanding: 0, shortfall);
    }

    public async Task<OffboardingCaseView> TransitionAsync(
        TransitionOffboardingCaseCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var entry = await LoadVisibleAsync(command.CaseId, actor, cancellationToken);
        var offboardingCase = entry.Case;
        if (offboardingCase.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = offboardingCase.Status;
        var blockingOutstanding = entry.BlockingTasksOutstanding;
        bool saved;

        switch (command.Action)
        {
            case OffboardingCaseAction.Approve:
            {
                RequireApprove(actor);
                var existingKeys = await repository.ListTemplateKeysAsync(offboardingCase.Id, cancellationToken);
                offboardingCase.Approve(actor.UserId, now);
                var tasks = OffboardingChecklistTemplate.Generate(
                    offboardingCase.Id,
                    offboardingCase.LastWorkingDate,
                    entry.Employee.ManagerUserId,
                    existingKeys,
                    now);
                saved = await repository.SaveApprovalAsync(
                    offboardingCase, previousStatus, tasks, command.ExpectedVersion, actor, cancellationToken);
                blockingOutstanding += tasks.Count(task => task.BlocksLastWorkingDay);
                break;
            }

            case OffboardingCaseAction.Start:
                RequireWrite(actor);
                offboardingCase.Start(now);
                saved = await repository.SaveTransitionAsync(
                    offboardingCase, previousStatus, command.ExpectedVersion, reason: null, actor, cancellationToken);
                break;

            case OffboardingCaseAction.Complete:
            {
                RequireWrite(actor);
                var blockingTasks = await repository.ListBlockingTasksAsync(offboardingCase.Id, cancellationToken);
                var completion = offboardingCase.Complete(
                    blockingTasks,
                    actorMayOverride: actor.HasPermission(OffboardingPermissions.Approve),
                    command.Reason,
                    entry.Employee.Status,
                    now);
                saved = await repository.SaveCompletionAsync(
                    offboardingCase,
                    completion.TerminationEvent,
                    command.ExpectedVersion,
                    completion.Overridden ? command.Reason?.Trim() : null,
                    actor,
                    cancellationToken);
                break;
            }

            case OffboardingCaseAction.Cancel:
                RequireWrite(actor);
                offboardingCase.Cancel(command.Reason, now);
                saved = await repository.SaveTransitionAsync(
                    offboardingCase, previousStatus, command.ExpectedVersion, command.Reason?.Trim(), actor, cancellationToken);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unknown offboarding case action.");
        }

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return new OffboardingCaseView(
            offboardingCase,
            blockingOutstanding,
            offboardingCase.NoticePeriodShortfallDays(entry.NoticePeriodDays));
    }

    private async Task<OffboardingCaseEntry> LoadVisibleAsync(long caseId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entry = await repository.GetByIdAsync(caseId, cancellationToken);
        if (entry is null || !actor.CanAccessEmployee(entry.Case.EmployeeId, entry.Employee.DepartmentId))
        {
            throw new CoreHrNotFoundException(ResourceName, caseId);
        }

        return entry;
    }

    private static void RequireWrite(CoreHrActor actor)
    {
        if (!actor.HasPermission(OffboardingPermissions.Write))
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Opening, starting, completing or cancelling an offboarding case requires the corehr.offboarding.write permission.");
        }
    }

    private static void RequireApprove(CoreHrActor actor)
    {
        if (!actor.HasPermission(OffboardingPermissions.Approve))
        {
            throw new CoreHrForbiddenException(
                ApproveForbiddenCode,
                "Approving an offboarding case requires the corehr.offboarding.approve permission.");
        }
    }

    private static CoreHrBusinessRuleException OpenCaseExists(long employeeId) => new(
        OffboardingCase.CaseOpenCode,
        $"Employee {employeeId} already has an open offboarding case.")
    {
        Details = new Dictionary<string, object?> { ["employeeId"] = employeeId }
    };

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}
