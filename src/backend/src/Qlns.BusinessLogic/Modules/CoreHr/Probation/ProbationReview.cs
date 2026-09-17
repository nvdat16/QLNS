using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>
/// Probation review (probation_reviews, EMP-06). Created in status <c>pending</c> by the Contracts module when a
/// probation contract is activated. Workflow: pending → in_review (reviewer submits) → decided (HR Manager) with
/// unlock back to in_review while the linked event is not applied; pending and in_review may be cancelled.
/// <para>
/// The schema has no <c>recommended_outcome</c> column: while the review is <c>in_review</c>, <see cref="Outcome"/> holds
/// the reviewer's recommendation (the proposed outcome); <see cref="Decide"/> overwrites it with the decided outcome and
/// <see cref="Unlock"/> keeps the last value as the recommendation again.
/// </para>
/// </summary>
public sealed class ProbationReview
{
    public const decimal MinScore = 0m;
    public const decimal MaxScore = 5m;
    public const int TextMaxLength = 4000;
    public const string DefaultDecisionReason = "Probation review decision";

    public const string InvalidTransitionCode = "corehr.probation.invalid_transition";
    public const string NotReviewedCode = "corehr.probation.not_reviewed";
    public const string NotReviewerCode = "corehr.probation.not_reviewer";
    public const string EventAppliedCode = "corehr.probation.event_applied";

    public long Id { get; }
    public long EmployeeId { get; }
    public long ContractId { get; }
    public DateOnly ReviewDueDate { get; }
    public long? ReviewerUserId { get; }
    public ProbationReviewStatus Status { get; private set; }
    public ProbationOutcome? Outcome { get; private set; }
    public decimal? OverallScore { get; private set; }
    public string? Strengths { get; private set; }
    public string? Improvements { get; private set; }
    public DateOnly? EffectiveDate { get; private set; }
    public long? DecidedBy { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public long? EmployeeEventId { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ProbationReview(
        long id,
        long employeeId,
        long contractId,
        DateOnly reviewDueDate,
        long? reviewerUserId,
        ProbationReviewStatus status,
        ProbationOutcome? outcome,
        decimal? overallScore,
        string? strengths,
        string? improvements,
        DateOnly? effectiveDate,
        long? decidedBy,
        DateTimeOffset? decidedAt,
        long? employeeEventId,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id <= 0 || employeeId <= 0 || contractId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persistent identifiers must be positive.");
        }

        if (reviewerUserId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reviewerUserId), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        Id = id;
        EmployeeId = employeeId;
        ContractId = contractId;
        ReviewDueDate = reviewDueDate;
        ReviewerUserId = reviewerUserId;
        Status = status;
        Outcome = outcome;
        OverallScore = overallScore;
        Strengths = strengths;
        Improvements = improvements;
        EffectiveDate = effectiveDate;
        DecidedBy = decidedBy;
        DecidedAt = decidedAt;
        EmployeeEventId = employeeEventId;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Legal-risk flag of EMP-06.1 scenario 4: past the due date while still pending or in_review.</summary>
    public bool IsOverdue(DateOnly today) => today > ReviewDueDate && Status.IsOpen();

    /// <summary>The assigned reviewer, or anyone holding <see cref="ProbationPermissions.Manage"/>, may submit the assessment.</summary>
    public bool CanBeAssessedBy(CoreHrActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return (ReviewerUserId is { } reviewer && reviewer == actor.UserId) ||
            actor.HasPermission(ProbationPermissions.Manage);
    }

    /// <summary>
    /// Records or replaces the assessment (EMP-06.1) and moves the review to in_review. Allowed from pending or
    /// in_review only. Recommending <c>terminated</c> requires improvement notes (422 on <c>improvements</c>).
    /// </summary>
    public void Submit(ProbationReviewWrite write, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);

        var errors = new ValidationErrors();

        if (write.OverallScore is not { } score)
        {
            errors.Add("overallScore", "overallScore is required.");
            score = 0;
        }
        else if (score < MinScore || score > MaxScore)
        {
            errors.Add("overallScore", "overallScore must be between 0 and 5.");
        }
        else if (decimal.Round(score, 1) != score)
        {
            errors.Add("overallScore", "overallScore accepts at most one decimal place.");
        }

        var strengths = write.Strengths?.Trim();
        if (string.IsNullOrEmpty(strengths))
        {
            errors.Add("strengths", "strengths is required.");
        }
        else if (strengths.Length > TextMaxLength)
        {
            errors.Add("strengths", $"strengths must be at most {TextMaxLength} characters.");
        }

        var improvements = string.IsNullOrWhiteSpace(write.Improvements) ? null : write.Improvements.Trim();
        if (improvements is { Length: > TextMaxLength })
        {
            errors.Add("improvements", $"improvements must be at most {TextMaxLength} characters.");
        }

        if (!ProbationOutcomeNames.TryParseContract(write.RecommendedOutcome, out var recommended))
        {
            errors.Add("recommendedOutcome", "recommendedOutcome must be one of confirmed, extended, terminated.");
        }
        else if (recommended == ProbationOutcome.Terminated && improvements is null)
        {
            errors.Add("improvements", "improvements is required when recommending termination.");
        }

        errors.ThrowIfAny();

        if (!Status.IsOpen())
        {
            throw InvalidTransition("submit");
        }

        OverallScore = decimal.Round(score, 1);
        Strengths = strengths;
        Improvements = improvements;
        Outcome = recommended;
        Status = ProbationReviewStatus.InReview;
        Touch(now);
    }

    /// <summary>
    /// EMP-06.2: in_review → decided. Requires outcome and effectiveDate (422, ck_probation_decided) with
    /// effectiveDate not in the past. Returns the approved employee event mapped from the outcome
    /// (confirmed → probation_confirmation/active, extended → probation_extension/probation,
    /// terminated → termination/terminated) and, for terminated, the draft offboarding case. Master data is
    /// untouched: the Effective-Date Worker applies the event.
    /// </summary>
    public ProbationDecisionPlan Decide(ProbationDecision decision, long decidedBy, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(decision);

        if (decidedBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(decidedBy));
        }

        if (Status == ProbationReviewStatus.Pending)
        {
            throw new CoreHrBusinessRuleException(
                NotReviewedCode,
                "The reviewer has not submitted the assessment yet; a pending review cannot be decided.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = Status.ToContract() }
            };
        }

        if (Status != ProbationReviewStatus.InReview)
        {
            throw InvalidTransition(ProbationReviewAction.Decide.ToContract());
        }

        var errors = new ValidationErrors();
        if (!ProbationOutcomeNames.TryParseContract(decision.Outcome, out var outcome))
        {
            errors.Add("outcome", "outcome is required and must be one of confirmed, extended, terminated.");
        }

        if (decision.EffectiveDate is not { } effectiveDate)
        {
            errors.Add("effectiveDate", "effectiveDate is required.");
            effectiveDate = today;
        }
        else if (effectiveDate < today)
        {
            errors.Add("effectiveDate", "effectiveDate cannot be in the past.");
        }

        var reason = ValidateReason(decision.Reason, errors) ?? DefaultDecisionReason;
        errors.ThrowIfAny();

        Status = ProbationReviewStatus.Decided;
        Outcome = outcome;
        EffectiveDate = effectiveDate;
        DecidedBy = decidedBy;
        DecidedAt = now;
        Touch(now);

        var (eventType, afterStatus) = outcome switch
        {
            ProbationOutcome.Confirmed => (EmployeeEventType.ProbationConfirmation, EmployeeStatusValues.Active),
            ProbationOutcome.Extended => (EmployeeEventType.ProbationExtension, EmployeeStatusValues.Probation),
            ProbationOutcome.Terminated => (EmployeeEventType.Termination, EmployeeStatusValues.Terminated),
            _ => throw new ArgumentOutOfRangeException(nameof(decision), "Unknown probation outcome.")
        };

        var employeeEvent = ApprovedEmployeeEvent.StatusChange(
            EmployeeId,
            eventType,
            effectiveDate,
            EmployeeStatusValues.Probation,
            afterStatus,
            reason);

        var offboardingCase = outcome == ProbationOutcome.Terminated
            ? OffboardingCase.OpenForProbationTermination(
                EmployeeId,
                effectiveDate,
                Improvements ?? reason,
                decidedBy,
                now)
            : null;

        return new ProbationDecisionPlan(employeeEvent, offboardingCase);
    }

    /// <summary>pending | in_review → cancelled with a mandatory reason.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        if (!Status.IsOpen())
        {
            throw InvalidTransition(ProbationReviewAction.Cancel.ToContract());
        }

        RequireReason(reason, "cancel");
        Status = ProbationReviewStatus.Cancelled;
        Touch(now);
    }

    /// <summary>
    /// decided → in_review with a mandatory reason, only while the linked employee event has not been applied
    /// (409 <see cref="EventAppliedCode"/>). Clears the decision columns; <see cref="Outcome"/> stays as the recommendation.
    /// Returns the id of the event that the repository cancels in the same transaction.
    /// </summary>
    public long Unlock(string? reason, EmployeeEventStatus? linkedEventStatus, DateTimeOffset now)
    {
        if (Status != ProbationReviewStatus.Decided)
        {
            throw InvalidTransition(ProbationReviewAction.Unlock.ToContract());
        }

        RequireReason(reason, "unlock");

        if (linkedEventStatus == EmployeeEventStatus.Applied)
        {
            throw new CoreHrBusinessRuleException(
                EventAppliedCode,
                $"Employee event {EmployeeEventId} has already been applied; the decision can only be corrected with a compensating event.")
            {
                Details = new Dictionary<string, object?> { ["employeeEventId"] = EmployeeEventId }
            };
        }

        var employeeEventId = EmployeeEventId
            ?? throw new InvalidOperationException($"Decided probation review {Id} has no linked employee event.");

        Status = ProbationReviewStatus.InReview;
        EmployeeEventId = null;
        DecidedBy = null;
        DecidedAt = null;
        EffectiveDate = null;
        Touch(now);
        return employeeEventId;
    }

    /// <summary>
    /// Persistence callback: the employee event's id is generated inside the decision transaction and linked to
    /// <c>employee_event_id</c> there; this mirrors the stored value without bumping the version.
    /// </summary>
    public void LinkEmployeeEvent(long employeeEventId)
    {
        if (employeeEventId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeEventId));
        }

        if (EmployeeEventId is { } existing && existing != employeeEventId)
        {
            throw new InvalidOperationException($"Probation review {Id} is already linked to employee event {existing}.");
        }

        EmployeeEventId = employeeEventId;
    }

    private static string? ValidateReason(string? value, ValidationErrors errors)
    {
        var reason = value?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            return null;
        }

        if (reason.Length > 1000)
        {
            errors.Add("reason", "reason must be at most 1000 characters.");
            return null;
        }

        return reason;
    }

    private static void RequireReason(string? reason, string action)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", $"A reason is required to {action} a probation review.");
        }
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    private CoreHrBusinessRuleException InvalidTransition(string action) => new(
        InvalidTransitionCode,
        $"Cannot {action} a probation review in status {Status.ToContract()}. " +
        "Allowed workflow: pending → in_review → decided; decided may be unlocked; pending and in_review may be cancelled.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action
        }
    };
}
