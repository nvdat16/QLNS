using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Offboarding case (offboarding_cases, EMP-07). Workflow: draft | pending_approval → approved → in_progress → completed;
/// every non-final status may be cancelled. Approval generates the checklist; completion is refused while blocking
/// tasks are outstanding (HR Manager override with reason) or the final settlement is not paid/waived (no override),
/// and on success yields the approved <c>termination</c> event applied by the Effective-Date Worker on the last working date.
/// </summary>
public sealed class OffboardingCase
{
    public const int ReasonMaxLength = 4000;
    public const string InvalidTransitionCode = "corehr.offboarding.invalid_transition";
    public const string BlockingTasksOutstandingCode = "corehr.offboarding.blocking_tasks_outstanding";
    public const string SettlementPendingCode = "corehr.offboarding.settlement_pending";
    public const string CaseOpenCode = "corehr.offboarding.case_open";
    public const string EmployeeNotEligibleCode = "corehr.offboarding.employee_not_eligible";

    /// <summary>employees.status values that may enter offboarding (SRS EMP-07 precondition). <c>suspended</c> is reserved.</summary>
    public static readonly IReadOnlySet<string> EligibleEmployeeStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        EmployeeStatusValues.Active, EmployeeStatusValues.Probation
    };

    public long Id { get; }
    public long EmployeeId { get; }
    public long? EmployeeEventId { get; private set; }
    public SeparationType SeparationType { get; }
    public DateOnly? NoticeReceivedOn { get; }
    public DateOnly LastWorkingDate { get; }
    public long? HandoverToEmployeeId { get; }
    public DateTimeOffset? ExitInterviewAt { get; }
    public FinalSettlementStatus FinalSettlementStatus { get; }
    public OffboardingCaseStatus Status { get; private set; }
    public string Reason { get; }
    public long CreatedBy { get; }
    public long? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public OffboardingCase(
        long id,
        long employeeId,
        long? employeeEventId,
        SeparationType separationType,
        DateOnly? noticeReceivedOn,
        DateOnly lastWorkingDate,
        long? handoverToEmployeeId,
        DateTimeOffset? exitInterviewAt,
        FinalSettlementStatus finalSettlementStatus,
        OffboardingCaseStatus status,
        string reason,
        long createdBy,
        long? approvedBy,
        DateTimeOffset? approvedAt,
        DateTimeOffset? completedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != OffboardingCaseStatus.Draft))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Only an unsaved draft may have id 0.");
        }

        if (employeeId <= 0 || createdBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Persistent identifiers must be positive.");
        }

        if (handoverToEmployeeId == employeeId)
        {
            throw new ArgumentException("The handover employee cannot be the leaving employee.", nameof(handoverToEmployeeId));
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        Id = id;
        EmployeeId = employeeId;
        EmployeeEventId = employeeEventId;
        SeparationType = separationType;
        NoticeReceivedOn = noticeReceivedOn;
        LastWorkingDate = lastWorkingDate;
        HandoverToEmployeeId = handoverToEmployeeId;
        ExitInterviewAt = exitInterviewAt;
        FinalSettlementStatus = finalSettlementStatus;
        Status = status;
        Reason = reason;
        CreatedBy = createdBy;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        CompletedAt = completedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the client payload (422 on failure) and builds an unsaved draft (Id = 0, Version = 1).</summary>
    public static OffboardingCase Open(OffboardingCaseWrite write, long createdBy, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        var errors = new ValidationErrors();

        if (write.EmployeeId <= 0)
        {
            errors.Add("employeeId", "employeeId must be a positive identifier.");
        }

        if (!SeparationTypeNames.TryParseContract(write.SeparationType, out var separationType))
        {
            errors.Add("separationType", "separationType must be one of resignation, mutual_agreement, dismissal, contract_expiry, retirement.");
        }

        if (write.LastWorkingDate == default)
        {
            errors.Add("lastWorkingDate", "lastWorkingDate is required.");
        }
        else if (write.LastWorkingDate < today)
        {
            errors.Add("lastWorkingDate", "lastWorkingDate cannot be in the past.");
        }

        if (write.NoticeReceivedOn is { } noticeReceivedOn && noticeReceivedOn > write.LastWorkingDate)
        {
            errors.Add("noticeReceivedOn", "noticeReceivedOn cannot be after lastWorkingDate.");
        }

        if (write.HandoverToEmployeeId is <= 0)
        {
            errors.Add("handoverToEmployeeId", "handoverToEmployeeId must be a positive identifier.");
        }
        else if (write.HandoverToEmployeeId == write.EmployeeId)
        {
            errors.Add("handoverToEmployeeId", "The handover employee cannot be the leaving employee.");
        }

        var reason = ValidateReason(write.Reason, errors);
        errors.ThrowIfAny();

        return new OffboardingCase(
            id: 0,
            write.EmployeeId,
            employeeEventId: null,
            separationType,
            write.NoticeReceivedOn,
            write.LastWorkingDate,
            write.HandoverToEmployeeId,
            exitInterviewAt: null,
            FinalSettlementStatus.Pending,
            OffboardingCaseStatus.Draft,
            reason!,
            createdBy,
            approvedBy: null,
            approvedAt: null,
            completedAt: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>
    /// Draft case opened automatically by a <c>terminated</c> probation decision (EMP-06.2 → EMP-07.1):
    /// separation type <c>dismissal</c>, last working date = the decision's effective date, no notice date.
    /// </summary>
    public static OffboardingCase OpenForProbationTermination(
        long employeeId,
        DateOnly effectiveDate,
        string reason,
        long createdBy,
        DateTimeOffset now)
    {
        var trimmed = reason?.Trim();
        ArgumentException.ThrowIfNullOrWhiteSpace(trimmed, nameof(reason));

        return new OffboardingCase(
            id: 0,
            employeeId,
            employeeEventId: null,
            SeparationType.Dismissal,
            noticeReceivedOn: null,
            effectiveDate,
            handoverToEmployeeId: null,
            exitInterviewAt: null,
            FinalSettlementStatus.Pending,
            OffboardingCaseStatus.Draft,
            trimmed.Length > ReasonMaxLength ? trimmed[..ReasonMaxLength] : trimmed,
            createdBy,
            approvedBy: null,
            approvedAt: null,
            completedAt: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    public static bool IsEligibleEmployeeStatus(string status) => EligibleEmployeeStatuses.Contains(status);

    /// <summary>Warning only: see <see cref="NoticePeriodRule"/>.</summary>
    public int? NoticePeriodShortfallDays(int? noticePeriodDays) =>
        NoticePeriodRule.ShortfallDays(NoticeReceivedOn, LastWorkingDate, noticePeriodDays);

    /// <summary>draft | pending_approval → approved. The caller generates the checklist in the same transaction.</summary>
    public void Approve(long approverUserId, DateTimeOffset now)
    {
        if (Status is not (OffboardingCaseStatus.Draft or OffboardingCaseStatus.PendingApproval))
        {
            throw InvalidTransition(OffboardingCaseAction.Approve);
        }

        Status = OffboardingCaseStatus.Approved;
        ApprovedBy = approverUserId;
        ApprovedAt = now;
        Touch(now);
    }

    /// <summary>approved → in_progress.</summary>
    public void Start(DateTimeOffset now)
    {
        if (Status != OffboardingCaseStatus.Approved)
        {
            throw InvalidTransition(OffboardingCaseAction.Start);
        }

        Status = OffboardingCaseStatus.InProgress;
        Touch(now);
    }

    /// <summary>
    /// in_progress → completed. Refused with <see cref="BlockingTasksOutstandingCode"/> while blocking tasks are
    /// incomplete unless <paramref name="actorMayOverride"/> and a reason is supplied; refused with
    /// <see cref="SettlementPendingCode"/> while the final settlement is not paid/waived (never overridable).
    /// Returns the approved termination event to insert, or null when one is already linked (idempotent replay).
    /// </summary>
    public OffboardingCompletion Complete(
        IReadOnlyList<OffboardingTask> tasks,
        bool actorMayOverride,
        string? reason,
        string currentEmployeeStatus,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tasks);

        if (Status != OffboardingCaseStatus.InProgress)
        {
            throw InvalidTransition(OffboardingCaseAction.Complete);
        }

        var outstanding = tasks.Where(task => task.IsBlockingOutstanding).ToList();
        var overridden = false;
        if (outstanding.Count > 0)
        {
            if (!actorMayOverride || string.IsNullOrWhiteSpace(reason))
            {
                throw new CoreHrBusinessRuleException(
                    BlockingTasksOutstandingCode,
                    $"{outstanding.Count} task(s) that block the last working day are not completed. " +
                    "Complete them, or override with the corehr.offboarding.approve permission and a reason.")
                {
                    Details = new Dictionary<string, object?>
                    {
                        ["blockingTasks"] = outstanding
                            .Select(task => new BlockingTaskSummary(task.Id, task.TemplateKey, task.TaskName, task.Status.ToContract()))
                            .ToList()
                    }
                };
            }

            overridden = true;
        }

        if (!FinalSettlementStatus.IsSettled())
        {
            throw new CoreHrBusinessRuleException(
                SettlementPendingCode,
                $"The final settlement is {FinalSettlementStatus.ToContract()}; it must be paid or waived before the case can be completed.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["finalSettlementStatus"] = FinalSettlementStatus.ToContract()
                }
            };
        }

        Status = OffboardingCaseStatus.Completed;
        CompletedAt = now;
        Touch(now);

        var terminationEvent = EmployeeEventId is null
            ? ApprovedEmployeeEvent.StatusChange(
                EmployeeId,
                EmployeeEventType.Termination,
                LastWorkingDate,
                currentEmployeeStatus,
                EmployeeStatusValues.Terminated,
                Reason)
            : null;

        return new OffboardingCompletion(terminationEvent, overridden);
    }

    /// <summary>Any status except completed | cancelled → cancelled, with a mandatory reason.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        if (Status is OffboardingCaseStatus.Completed or OffboardingCaseStatus.Cancelled)
        {
            throw InvalidTransition(OffboardingCaseAction.Cancel);
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to cancel an offboarding case.");
        }

        Status = OffboardingCaseStatus.Cancelled;
        Touch(now);
    }

    /// <summary>
    /// Persistence callback: the termination event's id is generated inside the completion transaction and
    /// linked to <c>employee_event_id</c> there; this mirrors the stored value without bumping the version.
    /// </summary>
    public void LinkEmployeeEvent(long employeeEventId)
    {
        if (employeeEventId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeEventId));
        }

        if (EmployeeEventId is { } existing && existing != employeeEventId)
        {
            throw new InvalidOperationException($"Offboarding case {Id} is already linked to employee event {existing}.");
        }

        EmployeeEventId = employeeEventId;
    }

    private static string? ValidateReason(string? value, ValidationErrors errors)
    {
        var reason = value?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            errors.Add("reason", "reason is required.");
            return null;
        }

        if (reason.Length > ReasonMaxLength)
        {
            errors.Add("reason", $"reason must be at most {ReasonMaxLength} characters.");
            return null;
        }

        return reason;
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    private CoreHrBusinessRuleException InvalidTransition(OffboardingCaseAction action) => new(
        InvalidTransitionCode,
        $"Cannot {action.ToContract()} an offboarding case in status {Status.ToContract()}. " +
        "Allowed workflow: draft → approved → in_progress → completed; open cases may be cancelled.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action.ToContract()
        }
    };
}

/// <summary>Result of <see cref="OffboardingCase.Complete"/>.</summary>
public sealed record OffboardingCompletion(ApprovedEmployeeEvent? TerminationEvent, bool Overridden);

/// <summary>Problem Details payload listing a task that still blocks completion.</summary>
public sealed record BlockingTaskSummary(long Id, string TemplateKey, string TaskName, string Status);
