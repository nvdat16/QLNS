using System.Globalization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>
/// Job requisition and, once published, the careers posting (REC-01, table job_postings).
/// Workflow: draft → pending_approval → approved → active_recruiting → closed; reject returns a pending requisition
/// to draft with a mandatory reason; cancel is allowed from every open state with a mandatory reason.
/// <see cref="TargetHeadcount"/>, <see cref="SalaryMin"/> and <see cref="SalaryMax"/> are declared data for the
/// approver: only their own validity is checked, never headcount plans or budgets. Every successful mutation bumps
/// <see cref="Version"/> and sets <see cref="UpdatedAt"/>.
/// </summary>
public sealed class Requisition
{
    public const int TitleMaxLength = 255;
    public const int LongTextMaxLength = 20000;
    public const int LocationMaxLength = 255;
    public const int ReasonMaxLength = 1000;

    public const string InvalidTransitionCode = "recruitment.requisition.invalid_transition";
    public const string NotEditableCode = "recruitment.requisition.not_editable";

    private static readonly RequisitionStatus[] CancellableStatuses =
    [
        RequisitionStatus.Draft,
        RequisitionStatus.PendingApproval,
        RequisitionStatus.Approved,
        RequisitionStatus.ActiveRecruiting
    ];

    public long Id { get; }

    /// <summary>Server-generated <c>REQ-{yyyy}-{id:D5}</c>; empty until the draft has been persisted.</summary>
    public string JobCode { get; }

    public string Title { get; private set; }
    public long DepartmentId { get; private set; }
    public long? PositionId { get; private set; }
    public string? Description { get; private set; }
    public string? Requirements { get; private set; }
    public string? Location { get; private set; }
    public EmploymentType EmploymentType { get; private set; }
    public decimal? SalaryMin { get; private set; }
    public decimal? SalaryMax { get; private set; }
    public int TargetHeadcount { get; private set; }
    public RequisitionStatus Status { get; private set; }
    public DateOnly? ClosingDate { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public long CreatedBy { get; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Requisition(
        long id,
        string jobCode,
        string title,
        long departmentId,
        long? positionId,
        string? description,
        string? requirements,
        string? location,
        EmploymentType employmentType,
        decimal? salaryMin,
        decimal? salaryMax,
        int targetHeadcount,
        RequisitionStatus status,
        DateOnly? closingDate,
        DateTimeOffset? publishedAt,
        long createdBy,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Identifier must be zero (transient) or positive.");
        }

        if (id > 0)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(jobCode);
        }

        if (departmentId <= 0 || createdBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(departmentId), "Persistent identifiers must be positive.");
        }

        if (positionId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(positionId), "Persistent identifiers must be positive.");
        }

        if (targetHeadcount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(targetHeadcount), "targetHeadcount must be at least 1.");
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be at least 1.");
        }

        ArgumentNullException.ThrowIfNull(jobCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        Id = id;
        JobCode = jobCode;
        Title = title;
        DepartmentId = departmentId;
        PositionId = positionId;
        Description = description;
        Requirements = requirements;
        Location = location;
        EmploymentType = employmentType;
        SalaryMin = salaryMin;
        SalaryMax = salaryMax;
        TargetHeadcount = targetHeadcount;
        Status = status;
        ClosingDate = closingDate;
        PublishedAt = publishedAt;
        CreatedBy = createdBy;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the payload (422) and returns a transient draft (id 0, no job code yet).</summary>
    public static Requisition CreateDraft(RequisitionWrite write, long createdBy, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        var fields = ValidatedFields.From(write, today);

        return new Requisition(
            id: 0,
            jobCode: string.Empty,
            fields.Title,
            fields.DepartmentId,
            fields.PositionId,
            fields.Description,
            fields.Requirements,
            fields.Location,
            fields.EmploymentType,
            fields.SalaryMin,
            fields.SalaryMax,
            fields.TargetHeadcount,
            RequisitionStatus.Draft,
            fields.ClosingDate,
            publishedAt: null,
            createdBy,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>Job code format of REC-01: <c>REQ-{yyyy}-{id:D5}</c>, unique because the id is.</summary>
    public static string BuildJobCode(int year, long id) =>
        string.Create(CultureInfo.InvariantCulture, $"REQ-{year:D4}-{id:D5}");

    /// <summary>
    /// Full replacement of the RequisitionWrite fields. Only drafts are editable (409 <see cref="NotEditableCode"/>).
    /// Returns the contract names of the fields whose value actually changed (used for the audit row).
    /// </summary>
    public IReadOnlyList<string> Replace(RequisitionWrite write, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);

        if (Status != RequisitionStatus.Draft)
        {
            throw new CoreHrBusinessRuleException(
                NotEditableCode,
                $"Only draft requisitions can be edited; this requisition is {Status.ToContract()}.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = Status.ToContract() }
            };
        }

        var fields = ValidatedFields.From(write, today);
        var changed = new List<string>();

        if (!string.Equals(Title, fields.Title, StringComparison.Ordinal))
        {
            Title = fields.Title;
            changed.Add("title");
        }

        if (DepartmentId != fields.DepartmentId)
        {
            DepartmentId = fields.DepartmentId;
            changed.Add("departmentId");
        }

        if (PositionId != fields.PositionId)
        {
            PositionId = fields.PositionId;
            changed.Add("positionId");
        }

        if (!string.Equals(Description, fields.Description, StringComparison.Ordinal))
        {
            Description = fields.Description;
            changed.Add("description");
        }

        if (!string.Equals(Requirements, fields.Requirements, StringComparison.Ordinal))
        {
            Requirements = fields.Requirements;
            changed.Add("requirements");
        }

        if (!string.Equals(Location, fields.Location, StringComparison.Ordinal))
        {
            Location = fields.Location;
            changed.Add("location");
        }

        if (EmploymentType != fields.EmploymentType)
        {
            EmploymentType = fields.EmploymentType;
            changed.Add("employmentType");
        }

        if (SalaryMin != fields.SalaryMin)
        {
            SalaryMin = fields.SalaryMin;
            changed.Add("salaryMin");
        }

        if (SalaryMax != fields.SalaryMax)
        {
            SalaryMax = fields.SalaryMax;
            changed.Add("salaryMax");
        }

        if (TargetHeadcount != fields.TargetHeadcount)
        {
            TargetHeadcount = fields.TargetHeadcount;
            changed.Add("targetHeadcount");
        }

        if (ClosingDate != fields.ClosingDate)
        {
            ClosingDate = fields.ClosingDate;
            changed.Add("closingDate");
        }

        Touch(now);
        return changed;
    }

    /// <summary>draft → pending_approval.</summary>
    public void Submit(DateTimeOffset now)
    {
        RequireStatus(RequisitionStatus.Draft, RequisitionAction.Submit);
        Status = RequisitionStatus.PendingApproval;
        Touch(now);
    }

    /// <summary>pending_approval → approved.</summary>
    public void Approve(DateTimeOffset now)
    {
        RequireStatus(RequisitionStatus.PendingApproval, RequisitionAction.Approve);
        Status = RequisitionStatus.Approved;
        Touch(now);
    }

    /// <summary>pending_approval → draft with a mandatory reason, so the hiring manager can revise and resubmit.</summary>
    public void Reject(string? reason, DateTimeOffset now)
    {
        RequireReason(reason, "reject");
        RequireStatus(RequisitionStatus.PendingApproval, RequisitionAction.Reject);
        Status = RequisitionStatus.Draft;
        Touch(now);
    }

    /// <summary>approved → active_recruiting; records <see cref="PublishedAt"/>. The closing date, when set, must be after today.</summary>
    public void Publish(DateOnly today, DateTimeOffset now)
    {
        RequireStatus(RequisitionStatus.Approved, RequisitionAction.Publish);

        if (ClosingDate is { } closingDate && closingDate <= today)
        {
            throw CoreHrValidationException.For("closingDate", "closingDate must be after the publication date.");
        }

        Status = RequisitionStatus.ActiveRecruiting;
        PublishedAt = now;
        Touch(now);
    }

    /// <summary>active_recruiting → closed.</summary>
    public void Close(DateTimeOffset now)
    {
        RequireStatus(RequisitionStatus.ActiveRecruiting, RequisitionAction.Close);
        Status = RequisitionStatus.Closed;
        Touch(now);
    }

    /// <summary>draft | pending_approval | approved | active_recruiting → cancelled with a mandatory reason.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        RequireReason(reason, "cancel");

        if (!CancellableStatuses.Contains(Status))
        {
            throw InvalidTransition(RequisitionAction.Cancel);
        }

        Status = RequisitionStatus.Cancelled;
        Touch(now);
    }

    /// <summary>Applies the transition requested by the API and returns the previous status.</summary>
    public RequisitionStatus Apply(RequisitionAction action, string? reason, DateOnly today, DateTimeOffset now)
    {
        var previous = Status;
        switch (action)
        {
            case RequisitionAction.Submit:
                Submit(now);
                break;
            case RequisitionAction.Approve:
                Approve(now);
                break;
            case RequisitionAction.Reject:
                Reject(reason, now);
                break;
            case RequisitionAction.Publish:
                Publish(today, now);
                break;
            case RequisitionAction.Close:
                Close(now);
                break;
            case RequisitionAction.Cancel:
                Cancel(reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }

        return previous;
    }

    /// <summary>Only reject and cancel record a reason; other actions ignore the optional body.</summary>
    public static bool RecordsReason(RequisitionAction action) =>
        action is RequisitionAction.Reject or RequisitionAction.Cancel;

    private void RequireStatus(RequisitionStatus expected, RequisitionAction action)
    {
        if (Status != expected)
        {
            throw InvalidTransition(action);
        }
    }

    private static void RequireReason(string? reason, string verb)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", $"A reason is required to {verb} a requisition.");
        }

        if (reason.Trim().Length > ReasonMaxLength)
        {
            throw CoreHrValidationException.For("reason", $"reason must be at most {ReasonMaxLength} characters.");
        }
    }

    private CoreHrBusinessRuleException InvalidTransition(RequisitionAction action) => new(
        InvalidTransitionCode,
        $"Cannot {action.ToContract()} a requisition in status {Status.ToContract()}. " +
        "Allowed workflow: draft → pending_approval → approved → active_recruiting → closed; " +
        "reject returns pending_approval to draft; cancel is allowed from every open status.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action.ToContract()
        }
    };

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    /// <summary>Semantic validation of RequisitionWrite (422 with contract field names). Trims text fields.</summary>
    private sealed record ValidatedFields(
        string Title,
        long DepartmentId,
        long? PositionId,
        string? Description,
        string? Requirements,
        string? Location,
        EmploymentType EmploymentType,
        decimal? SalaryMin,
        decimal? SalaryMax,
        int TargetHeadcount,
        DateOnly? ClosingDate)
    {
        public static ValidatedFields From(RequisitionWrite write, DateOnly today)
        {
            var errors = new ValidationErrors();

            var title = write.Title?.Trim();
            if (string.IsNullOrEmpty(title))
            {
                errors.Add("title", "title is required.");
            }
            else if (title.Length > TitleMaxLength)
            {
                errors.Add("title", $"title must be at most {TitleMaxLength} characters.");
            }

            if (write.DepartmentId <= 0)
            {
                errors.Add("departmentId", "departmentId must be a positive identifier.");
            }

            if (write.PositionId is <= 0)
            {
                errors.Add("positionId", "positionId must be a positive identifier.");
            }

            var description = NormalizeText(write.Description);
            if (description is { Length: > LongTextMaxLength })
            {
                errors.Add("description", $"description must be at most {LongTextMaxLength} characters.");
            }

            var requirements = NormalizeText(write.Requirements);
            if (requirements is { Length: > LongTextMaxLength })
            {
                errors.Add("requirements", $"requirements must be at most {LongTextMaxLength} characters.");
            }

            var location = NormalizeText(write.Location);
            if (location is { Length: > LocationMaxLength })
            {
                errors.Add("location", $"location must be at most {LocationMaxLength} characters.");
            }

            if (!EmploymentTypeNames.TryParseContract(write.EmploymentType, out var employmentType))
            {
                errors.Add("employmentType", $"employmentType must be one of {string.Join(", ", EmploymentTypeNames.ContractValues)}.");
            }

            if (write.SalaryMin is < 0)
            {
                errors.Add("salaryMin", "salaryMin must not be negative.");
            }

            if (write.SalaryMax is < 0)
            {
                errors.Add("salaryMax", "salaryMax must not be negative.");
            }

            if (write.SalaryMin is { } min && write.SalaryMax is { } max && min >= 0 && max >= 0 && min > max)
            {
                errors.Add("salaryMin", "salaryMin must not exceed salaryMax.");
            }

            if (write.TargetHeadcount < 1)
            {
                errors.Add("targetHeadcount", "targetHeadcount must be at least 1.");
            }

            if (write.ClosingDate is { } closingDate && closingDate < today)
            {
                errors.Add("closingDate", "closingDate must not be in the past.");
            }

            errors.ThrowIfAny();

            return new ValidatedFields(
                title!,
                write.DepartmentId,
                write.PositionId,
                description,
                requirements,
                location,
                employmentType,
                write.SalaryMin,
                write.SalaryMax,
                write.TargetHeadcount,
                write.ClosingDate);
        }

        private static string? NormalizeText(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
