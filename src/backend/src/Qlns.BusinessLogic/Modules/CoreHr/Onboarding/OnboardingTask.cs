using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>
/// Onboarding checklist item (EMP-03). Workflow is strictly pending → in_progress → completed;
/// a completed task can be reopened to pending with a mandatory reason. Every successful mutation
/// bumps <see cref="Version"/> and sets <see cref="UpdatedAt"/>.
/// </summary>
public sealed class OnboardingTask
{
    public const int TaskNameMaxLength = 255;
    public const int DescriptionMaxLength = 5000;
    public const string InvalidTransitionCode = "corehr.onboarding.invalid_transition";

    public long Id { get; }
    public long EmployeeId { get; }
    public string TemplateKey { get; }
    public string TaskName { get; private set; }
    public string? Description { get; private set; }
    public long? AssignedToUserId { get; private set; }
    public DateTimeOffset? DueAt { get; private set; }
    public OnboardingTaskStatus Status { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public OnboardingTask(
        long id,
        long employeeId,
        string templateKey,
        string taskName,
        string? description,
        long? assignedToUserId,
        DateTimeOffset? dueAt,
        OnboardingTaskStatus status,
        DateTimeOffset? completedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id <= 0 || employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persistent identifiers must be positive.");
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
        EmployeeId = employeeId;
        TemplateKey = templateKey;
        TaskName = taskName;
        Description = description;
        AssignedToUserId = assignedToUserId;
        DueAt = dueAt;
        Status = status;
        CompletedAt = completedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>pending → in_progress.</summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != OnboardingTaskStatus.Pending)
        {
            throw InvalidTransition(OnboardingTaskAction.Start);
        }

        Status = OnboardingTaskStatus.InProgress;
        Touch(now);
    }

    /// <summary>in_progress → completed; records <see cref="CompletedAt"/>.</summary>
    public void Complete(DateTimeOffset now)
    {
        if (Status != OnboardingTaskStatus.InProgress)
        {
            throw InvalidTransition(OnboardingTaskAction.Complete);
        }

        Status = OnboardingTaskStatus.Completed;
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

        if (Status != OnboardingTaskStatus.Completed)
        {
            throw InvalidTransition(OnboardingTaskAction.Reopen);
        }

        Status = OnboardingTaskStatus.Pending;
        CompletedAt = null;
        Touch(now);
    }

    /// <summary>Applies the transition requested by the API and returns the previous status.</summary>
    public OnboardingTaskStatus Apply(OnboardingTaskAction action, string? reason, DateTimeOffset now)
    {
        var previous = Status;
        switch (action)
        {
            case OnboardingTaskAction.Start:
                Start(now);
                break;
            case OnboardingTaskAction.Complete:
                Complete(now);
                break;
            case OnboardingTaskAction.Reopen:
                Reopen(reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        return previous;
    }

    /// <summary>
    /// Replaces name, description, assignee and due date. Returns the contract names of the fields
    /// whose value actually changed (used for the audit row).
    /// </summary>
    public IReadOnlyList<string> UpdateAssignment(OnboardingTaskWrite write, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);

        var errors = new ValidationErrors();
        var taskName = write.TaskName?.Trim();
        if (string.IsNullOrEmpty(taskName))
        {
            errors.Add("taskName", "taskName is required.");
        }
        else if (taskName.Length > TaskNameMaxLength)
        {
            errors.Add("taskName", $"taskName must be at most {TaskNameMaxLength} characters.");
        }

        var description = string.IsNullOrWhiteSpace(write.Description) ? null : write.Description.Trim();
        if (description is { Length: > DescriptionMaxLength })
        {
            errors.Add("description", $"description must be at most {DescriptionMaxLength} characters.");
        }

        if (write.AssignedToUserId is <= 0)
        {
            errors.Add("assignedToUserId", "assignedToUserId must be a positive identifier.");
        }

        errors.ThrowIfAny();

        var changed = new List<string>();
        if (!string.Equals(TaskName, taskName, StringComparison.Ordinal))
        {
            TaskName = taskName!;
            changed.Add("taskName");
        }

        if (!string.Equals(Description, description, StringComparison.Ordinal))
        {
            Description = description;
            changed.Add("description");
        }

        if (AssignedToUserId != write.AssignedToUserId)
        {
            AssignedToUserId = write.AssignedToUserId;
            changed.Add("assignedToUserId");
        }

        if (DueAt != write.DueAt)
        {
            DueAt = write.DueAt;
            changed.Add("dueAt");
        }

        Touch(now);
        return changed;
    }

    public bool IsOverdue(DateTimeOffset now) =>
        Status != OnboardingTaskStatus.Completed && DueAt.HasValue && DueAt.Value < now;

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    private CoreHrBusinessRuleException InvalidTransition(OnboardingTaskAction action) => new(
        InvalidTransitionCode,
        $"Cannot {action.ToContract()} an onboarding task in status {Status.ToContract()}. " +
        "Allowed workflow: pending → in_progress → completed; only completed tasks can be reopened.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action.ToContract()
        }
    };
}
