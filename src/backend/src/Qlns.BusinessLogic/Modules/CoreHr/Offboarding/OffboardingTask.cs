using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Handover / asset-recovery checklist item of an offboarding case (EMP-07.2). Workflow is strictly
/// pending → in_progress → completed; a completed task can be reopened to pending with a mandatory reason.
/// Tasks marked <see cref="BlocksLastWorkingDay"/> must be completed before the case can be closed.
/// </summary>
public sealed class OffboardingTask
{
    public const string InvalidTransitionCode = "corehr.offboarding.task.invalid_transition";

    public long Id { get; }
    public long OffboardingCaseId { get; }
    public string TemplateKey { get; }
    public OffboardingTaskCategory Category { get; }
    public string TaskName { get; }
    public string? Description { get; }
    public long? AssignedToUserId { get; }
    public DateTimeOffset? DueAt { get; }
    public bool BlocksLastWorkingDay { get; }
    public OffboardingTaskStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public OffboardingTask(
        long id,
        long offboardingCaseId,
        string templateKey,
        OffboardingTaskCategory category,
        string taskName,
        string? description,
        long? assignedToUserId,
        DateTimeOffset? dueAt,
        bool blocksLastWorkingDay,
        OffboardingTaskStatus status,
        DateTimeOffset? completedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != OffboardingTaskStatus.Pending))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Only an unsaved pending task may have id 0.");
        }

        if (offboardingCaseId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offboardingCaseId), "Persistent identifiers must be positive.");
        }

        if (assignedToUserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(assignedToUserId), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(taskName);

        Id = id;
        OffboardingCaseId = offboardingCaseId;
        TemplateKey = templateKey;
        Category = category;
        TaskName = taskName;
        Description = description;
        AssignedToUserId = assignedToUserId;
        DueAt = dueAt;
        BlocksLastWorkingDay = blocksLastWorkingDay;
        Status = status;
        CompletedAt = completedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>True while the task still prevents closing the case.</summary>
    public bool IsBlockingOutstanding => BlocksLastWorkingDay && Status != OffboardingTaskStatus.Completed;

    /// <summary>pending → in_progress.</summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != OffboardingTaskStatus.Pending)
        {
            throw InvalidTransition(OffboardingTaskAction.Start);
        }

        Status = OffboardingTaskStatus.InProgress;
        Touch(now);
    }

    /// <summary>in_progress → completed; records <see cref="CompletedAt"/>.</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status != OffboardingTaskStatus.InProgress)
        {
            throw InvalidTransition(OffboardingTaskAction.Complete);
        }

        Status = OffboardingTaskStatus.Completed;
        CompletedAt = now;
        Touch(now);
    }

    /// <summary>completed → pending with a mandatory reason; clears <see cref="CompletedAt"/>.</summary>
    public void Reopen(string? reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to reopen a completed task.");
        }

        if (Status != OffboardingTaskStatus.Completed)
        {
            throw InvalidTransition(OffboardingTaskAction.Reopen);
        }

        Status = OffboardingTaskStatus.Pending;
        CompletedAt = null;
        Touch(now);
    }

    /// <summary>Applies the transition requested by the API and returns the previous status.</summary>
    public OffboardingTaskStatus Apply(OffboardingTaskAction action, string? reason, DateTimeOffset now)
    {
        var previous = Status;
        switch (action)
        {
            case OffboardingTaskAction.Start:
                Start(now);
                break;
            case OffboardingTaskAction.Complete:
                Complete(now);
                break;
            case OffboardingTaskAction.Reopen:
                Reopen(reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        return previous;
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    private CoreHrBusinessRuleException InvalidTransition(OffboardingTaskAction action) => new(
        InvalidTransitionCode,
        $"Cannot {action.ToContract()} an offboarding task in status {Status.ToContract()}. " +
        "Allowed workflow: pending → in_progress → completed; only completed tasks can be reopened.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action.ToContract()
        }
    };
}
