using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>
/// Interview aggregate (tables <c>interviews</c> + <c>interview_panelists</c>, REC-04.1). Workflow:
/// scheduled → completed | cancelled; a scheduled interview may be rescheduled any number of times.
/// The first panelist is the lead interviewer (<c>interviews.interviewer_user_id</c>).
/// Every successful mutation bumps <see cref="Version"/> and sets <see cref="UpdatedAt"/>.
/// </summary>
public sealed class Interview
{
    public const int InterviewTypeMaxLength = 50;
    public const int ReasonMaxLength = 1000;
    public const string InvalidTransitionCode = "recruitment.interview.invalid_transition";
    public const string NotStartedCode = "recruitment.interview.not_started";

    public long Id { get; }
    public long ApplicationId { get; }
    public string InterviewType { get; }
    public InterviewSlot Slot { get; private set; }
    public DateTimeOffset StartsAt => Slot.StartsAt;
    public DateTimeOffset EndsAt => Slot.EndsAt;
    public string Timezone { get; private set; }
    public IReadOnlyList<long> PanelUserIds { get; }
    public long LeadInterviewerUserId => PanelUserIds[0];
    public string? Location { get; private set; }
    public string? MeetingUrl { get; private set; }
    public InterviewStatus Status { get; private set; }
    public string? CancellationReason { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Interview(
        long id,
        long applicationId,
        string interviewType,
        InterviewSlot slot,
        string timezone,
        IReadOnlyList<long> panelUserIds,
        string? location,
        string? meetingUrl,
        InterviewStatus status,
        string? cancellationReason,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != InterviewStatus.Scheduled))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Only an unsaved scheduled interview may have id 0.");
        }

        if (applicationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(applicationId), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        ArgumentNullException.ThrowIfNull(panelUserIds);
        if (panelUserIds.Count == 0 || panelUserIds.Any(userId => userId <= 0) || panelUserIds.Distinct().Count() != panelUserIds.Count)
        {
            throw new ArgumentException("The panel must contain at least one distinct positive user id.", nameof(panelUserIds));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(interviewType);
        ArgumentException.ThrowIfNullOrWhiteSpace(timezone);

        Id = id;
        ApplicationId = applicationId;
        InterviewType = interviewType;
        Slot = slot;
        Timezone = timezone;
        PanelUserIds = panelUserIds;
        Location = location;
        MeetingUrl = meetingUrl;
        Status = status;
        CancellationReason = cancellationReason;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the client payload (422 on failure) and builds an unsaved scheduled interview (Id = 0, Version = 1).</summary>
    public static Interview Schedule(InterviewWrite write, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        var errors = new ValidationErrors();

        if (write.ApplicationId <= 0)
        {
            errors.Add("applicationId", "applicationId must be a positive identifier.");
        }

        var interviewType = write.InterviewType?.Trim();
        if (string.IsNullOrEmpty(interviewType))
        {
            errors.Add("interviewType", "interviewType is required.");
        }
        else if (interviewType.Length > InterviewTypeMaxLength)
        {
            errors.Add("interviewType", $"interviewType must be at most {InterviewTypeMaxLength} characters.");
        }

        var slot = InterviewScheduleRules.ValidateSlot(write.StartsAt, write.EndsAt, now, errors);
        var timezone = InterviewScheduleRules.ValidateTimezone(write.Timezone, errors);
        var (location, meetingUrl) = InterviewScheduleRules.ValidateVenue(write.Location, write.MeetingUrl, errors);

        var panel = write.InterviewerUserIds ?? [];
        if (panel.Count == 0)
        {
            errors.Add("interviewerUserIds", "At least one interviewer is required.");
        }
        else if (panel.Any(userId => userId <= 0))
        {
            errors.Add("interviewerUserIds", "interviewerUserIds must contain positive identifiers.");
        }
        else if (panel.Distinct().Count() != panel.Count)
        {
            errors.Add("interviewerUserIds", "interviewerUserIds must not contain duplicates.");
        }

        errors.ThrowIfAny();

        return new Interview(
            id: 0,
            write.ApplicationId,
            interviewType!,
            slot!.Value,
            timezone!,
            panel.ToList(),
            location,
            meetingUrl,
            InterviewStatus.Scheduled,
            cancellationReason: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    public bool IsPanelist(long userId) => PanelUserIds.Contains(userId);

    /// <summary>Moves a scheduled interview to a new slot; null timezone/venue fields keep their current value.</summary>
    public void Reschedule(InterviewChange? change, DateTimeOffset now)
    {
        RequireScheduled(InterviewAction.Reschedule);
        change ??= InterviewChange.Empty;

        var errors = new ValidationErrors();
        var slot = InterviewScheduleRules.ValidateSlot(change.StartsAt, change.EndsAt, now, errors);
        var timezone = change.Timezone is null ? Timezone : InterviewScheduleRules.ValidateTimezone(change.Timezone, errors);
        var (location, meetingUrl) = InterviewScheduleRules.ValidateVenue(
            change.Location ?? Location,
            change.MeetingUrl ?? MeetingUrl,
            errors);
        errors.ThrowIfAny();

        Slot = slot!.Value;
        Timezone = timezone!;
        Location = location;
        MeetingUrl = meetingUrl;
        Touch(now);
    }

    /// <summary>scheduled → completed, only once the interview has started.</summary>
    public void Complete(DateTimeOffset now)
    {
        RequireScheduled(InterviewAction.Complete);
        if (now < Slot.StartsAt)
        {
            throw new CoreHrBusinessRuleException(
                NotStartedCode,
                $"Interview {Id} starts at {Slot.StartsAt:O} and cannot be completed before it has started.");
        }

        Status = InterviewStatus.Completed;
        Touch(now);
    }

    /// <summary>scheduled → cancelled with a mandatory reason (kept for history, REC-04).</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to cancel an interview.");
        }

        if (trimmed.Length > ReasonMaxLength)
        {
            throw CoreHrValidationException.For("reason", $"reason must be at most {ReasonMaxLength} characters.");
        }

        RequireScheduled(InterviewAction.Cancel);
        Status = InterviewStatus.Cancelled;
        CancellationReason = trimmed;
        Touch(now);
    }

    /// <summary>Schedule-relevant state before a mutation; repositories record it as the audit "before" payload.</summary>
    public InterviewSnapshot Snapshot() => new(Slot, Timezone, Location, MeetingUrl, Status, Version);

    private void RequireScheduled(InterviewAction action)
    {
        if (Status != InterviewStatus.Scheduled)
        {
            throw new CoreHrBusinessRuleException(
                InvalidTransitionCode,
                $"Cannot {action.ToContract()} an interview in status {Status.ToContract()}; only scheduled interviews can change.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["currentStatus"] = Status.ToContract(),
                    ["action"] = action.ToContract()
                }
            };
        }
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }
}

/// <summary>Immutable copy of the mutable interview fields, used for audit before/after payloads.</summary>
public sealed record InterviewSnapshot(
    InterviewSlot Slot,
    string Timezone,
    string? Location,
    string? MeetingUrl,
    InterviewStatus Status,
    long Version);
