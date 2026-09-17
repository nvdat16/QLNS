using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>
/// Employee movement event (employee_events). Workflow: draft → pending_approval → approved → applied;
/// draft, pending_approval and approved may be cancelled. Applied events are immutable — history is corrected
/// with a compensating <c>correction</c> event. <see cref="ApplyTo"/> is the only place master data changes.
/// </summary>
public sealed class EmployeeEvent
{
    public const int ReasonMaxLength = 5000;

    /// <summary>afterData keys accepted by EmployeeEventWrite. departmentId/positionId/managerId/status are applied to employees.</summary>
    public static readonly IReadOnlySet<string> AllowedFields = new HashSet<string>(StringComparer.Ordinal)
    {
        "departmentId", "positionId", "managerId", "status", "salary", "grade", "officeLocation", "workEmail"
    };

    public long Id { get; }
    public long EmployeeId { get; }
    public EmployeeEventType EventType { get; }
    public EmployeeEventStatus Status { get; private set; }
    public DateOnly EffectiveDate { get; }
    public JsonObject BeforeData { get; }
    public JsonObject AfterData { get; }
    public string Reason { get; }
    public long? CompensatesEventId { get; }
    public long CreatedBy { get; }
    public long? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Names of the fields this event changes (afterData property names).</summary>
    public IReadOnlyList<string> ChangedFields => AfterData.Select(pair => pair.Key).ToList();

    public EmployeeEvent(
        long id,
        long employeeId,
        EmployeeEventType eventType,
        EmployeeEventStatus status,
        DateOnly effectiveDate,
        JsonObject beforeData,
        JsonObject afterData,
        string reason,
        long? compensatesEventId,
        long createdBy,
        long? approvedBy,
        DateTimeOffset? approvedAt,
        DateTimeOffset? appliedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != EmployeeEventStatus.Draft))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Only an unsaved draft may have id 0.");
        }

        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Id = id;
        EmployeeId = employeeId;
        EventType = eventType;
        Status = status;
        EffectiveDate = effectiveDate;
        BeforeData = beforeData ?? throw new ArgumentNullException(nameof(beforeData));
        AfterData = afterData ?? throw new ArgumentNullException(nameof(afterData));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        CompensatesEventId = compensatesEventId;
        CreatedBy = createdBy;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        AppliedAt = appliedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the client payload (422 on failure) and builds an unsaved draft (Id = 0, Version = 1).</summary>
    public static EmployeeEvent CreateDraft(long employeeId, EmployeeEventWrite write, long createdBy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        var errors = new ValidationErrors();

        var typeKnown = EmployeeEventTypeNames.TryParseContract(write.EventType, out var eventType);
        if (!typeKnown || !EmployeeEventTypeNames.ClientWritable.Contains(eventType))
        {
            errors.Add("eventType", "eventType must be one of promotion, transfer, demotion, salary_adjustment, termination, correction.");
        }

        if (write.EffectiveDate == default)
        {
            errors.Add("effectiveDate", "effectiveDate is required.");
        }

        if (write.BeforeData is null)
        {
            errors.Add("beforeData", "beforeData must be a JSON object.");
        }

        if (write.AfterData is null)
        {
            errors.Add("afterData", "afterData must be a JSON object.");
        }
        else
        {
            ValidateAfterData(write.AfterData, errors);
        }

        var reason = write.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            errors.Add("reason", "reason is required.");
        }
        else if (reason.Length > ReasonMaxLength)
        {
            errors.Add("reason", $"reason must be at most {ReasonMaxLength} characters.");
        }

        if (typeKnown && eventType == EmployeeEventType.Termination && write.AfterData is not null &&
            !(TryGetString(write.AfterData["status"], out var terminationStatus) && terminationStatus == EmployeeStatusValues.Terminated))
        {
            errors.Add("afterData.status", "termination requires afterData.status to be \"terminated\".");
        }

        if (typeKnown && eventType == EmployeeEventType.Correction && write.CompensatesEventId is null)
        {
            errors.Add("compensatesEventId", "correction requires compensatesEventId of the applied event it compensates.");
        }

        if (write.CompensatesEventId is <= 0)
        {
            errors.Add("compensatesEventId", "compensatesEventId must be a positive identifier.");
        }

        errors.ThrowIfAny();

        return new EmployeeEvent(
            id: 0,
            employeeId,
            eventType,
            EmployeeEventStatus.Draft,
            write.EffectiveDate,
            write.BeforeData!.DeepClone().AsObject(),
            write.AfterData!.DeepClone().AsObject(),
            reason!,
            write.CompensatesEventId,
            createdBy,
            approvedBy: null,
            approvedAt: null,
            appliedAt: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    public void Submit(DateTimeOffset now)
    {
        RequireStatus(EmployeeEventStatus.Draft, "submit");
        Status = EmployeeEventStatus.PendingApproval;
        Touch(now);
    }

    public void Approve(long approverUserId, DateTimeOffset now)
    {
        RequireStatus(EmployeeEventStatus.PendingApproval, "approve");
        Status = EmployeeEventStatus.Approved;
        ApprovedBy = approverUserId;
        ApprovedAt = now;
        Touch(now);
    }

    public void Cancel(string? reason, DateTimeOffset now)
    {
        if (Status == EmployeeEventStatus.Applied)
        {
            throw new CoreHrBusinessRuleException(
                "corehr.event.immutable",
                "Applied events are immutable; create a compensating correction event.");
        }

        if (Status == EmployeeEventStatus.Cancelled)
        {
            throw InvalidTransition("cancel");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to cancel an event.");
        }

        Status = EmployeeEventStatus.Cancelled;
        Touch(now);
    }

    public bool IsDue(DateOnly today) => Status == EmployeeEventStatus.Approved && EffectiveDate <= today;

    /// <summary>
    /// Applies the approved event to the employee snapshot and marks the event applied. The only place where
    /// department, position, manager and status of an employee change (architecture §5.5).
    /// </summary>
    public EmployeeMasterData ApplyTo(EmployeeMasterData current, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (!IsDue(today))
        {
            throw new CoreHrBusinessRuleException(
                "corehr.event.not_due",
                $"Event {Id} is not approved or its effective date {EffectiveDate:yyyy-MM-dd} is after {today:yyyy-MM-dd}.");
        }

        if (current.EmployeeId != EmployeeId)
        {
            throw new ArgumentException("Master data belongs to a different employee.", nameof(current));
        }

        var departmentId = ReadId("departmentId") ?? current.DepartmentId;
        var positionId = ReadId("positionId") ?? current.PositionId;
        var managerId = AfterData.ContainsKey("managerId") ? ReadId("managerId") : current.ManagerId;
        var status = TryGetString(AfterData["status"], out var newStatus) ? newStatus : current.Status;
        var workEmail = AfterData.ContainsKey("workEmail")
            ? (TryGetString(AfterData["workEmail"], out var newEmail) ? newEmail : null)
            : current.WorkEmail;

        if (!EmployeeStatusValues.IsValid(status))
        {
            throw InvalidAfterData("status");
        }

        if (managerId == EmployeeId)
        {
            throw new CoreHrBusinessRuleException(
                "corehr.event.self_manager",
                "An employee cannot be their own manager.");
        }

        if (status == EmployeeStatusValues.Active && string.IsNullOrWhiteSpace(workEmail))
        {
            throw new CoreHrBusinessRuleException(
                "corehr.event.active_requires_work_email",
                "An employee must have a work email before becoming active.");
        }

        Status = EmployeeEventStatus.Applied;
        AppliedAt = now;
        Touch(now);

        return current with
        {
            DepartmentId = departmentId,
            PositionId = positionId,
            ManagerId = managerId,
            Status = status,
            WorkEmail = workEmail,
            Version = current.Version + 1
        };
    }

    private long? ReadId(string field)
    {
        if (!AfterData.TryGetPropertyValue(field, out var node) || node is null)
        {
            return null;
        }

        return TryGetPositiveLong(node, out var value) ? value : throw InvalidAfterData(field);
    }

    private static void ValidateAfterData(JsonObject afterData, ValidationErrors errors)
    {
        if (afterData.Count == 0)
        {
            errors.Add("afterData", "afterData must contain at least one changed field.");
            return;
        }

        foreach (var (key, value) in afterData)
        {
            if (!AllowedFields.Contains(key))
            {
                errors.Add($"afterData.{key}", "Unknown field. Allowed: " + string.Join(", ", AllowedFields) + ".");
                continue;
            }

            switch (key)
            {
                case "departmentId" or "positionId" when !TryGetPositiveLong(value, out _):
                    errors.Add($"afterData.{key}", "Must be a positive integer identifier.");
                    break;
                case "managerId" when value is not null && !TryGetPositiveLong(value, out _):
                    errors.Add("afterData.managerId", "Must be a positive integer identifier or null.");
                    break;
                case "status" when !(TryGetString(value, out var status) && EmployeeStatusValues.IsValid(status)):
                    errors.Add("afterData.status", "Must be one of probation, active, suspended, terminated.");
                    break;
                case "workEmail" when value is not null && !TryGetString(value, out _):
                    errors.Add("afterData.workEmail", "Must be a string or null.");
                    break;
            }
        }
    }

    private static bool TryGetPositiveLong(JsonNode? node, out long value)
    {
        value = 0;
        return node is JsonValue &&
            node.GetValueKind() == JsonValueKind.Number &&
            long.TryParse(node.ToJsonString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value) &&
            value > 0;
    }

    private static bool TryGetString(JsonNode? node, out string value)
    {
        value = string.Empty;
        if (node is JsonValue && node.GetValueKind() == JsonValueKind.String && node.AsValue().TryGetValue<string>(out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private void RequireStatus(EmployeeEventStatus expected, string action)
    {
        if (Status != expected)
        {
            throw InvalidTransition(action);
        }
    }

    private CoreHrBusinessRuleException InvalidTransition(string action) => new(
        "corehr.event.invalid_transition",
        $"Cannot {action} an event in status {Status.ToContract()}.");

    private CoreHrBusinessRuleException InvalidAfterData(string field) => new(
        "corehr.event.invalid_after_data",
        $"Event {Id} has an invalid afterData.{field} value and cannot be applied.");

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }
}
