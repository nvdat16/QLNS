using System.Text.RegularExpressions;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>
/// Offer aggregate (table <c>offers</c>, REC-06.1). Workflow: draft → approved → sent → accepted | declined | expired;
/// draft, approved and sent may be cancelled; sent and expired may be extended (back to sent) with a new expiration
/// date. A <c>sent</c> offer whose expiration date has passed is expired lazily by the service or by the worker.
/// Every successful mutation bumps <see cref="Version"/> (the ETag) and sets <see cref="UpdatedAt"/>.
/// </summary>
public sealed partial class Offer
{
    public const string DefaultCurrency = "VND";
    public const int TemplateVersionMaxLength = 50;
    public const int ReasonMaxLength = 1000;
    public const string InvalidTransitionCode = "recruitment.offer.invalid_transition";

    public long Id { get; }
    public long ApplicationId { get; }
    public decimal BaseSalary { get; }
    public decimal? BonusAmount { get; }
    public decimal? AllowanceAmount { get; }
    public string Currency { get; }
    public string EmploymentType { get; }
    public DateOnly StartDate { get; }
    public DateOnly ExpirationDate { get; private set; }
    public OfferStatus Status { get; private set; }
    public string TemplateVersion { get; }
    public string? DocumentObjectKey { get; }
    public long? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool HasDocument => DocumentObjectKey is not null;

    public Offer(
        long id,
        long applicationId,
        decimal baseSalary,
        decimal? bonusAmount,
        decimal? allowanceAmount,
        string currency,
        string employmentType,
        DateOnly startDate,
        DateOnly expirationDate,
        OfferStatus status,
        string templateVersion,
        string? documentObjectKey,
        long? approvedBy,
        DateTimeOffset? approvedAt,
        DateTimeOffset? sentAt,
        DateTimeOffset? respondedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != OfferStatus.Draft))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Only an unsaved draft may have id 0.");
        }

        if (applicationId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(applicationId), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        ArgumentException.ThrowIfNullOrWhiteSpace(employmentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(templateVersion);

        Id = id;
        ApplicationId = applicationId;
        BaseSalary = baseSalary;
        BonusAmount = bonusAmount;
        AllowanceAmount = allowanceAmount;
        Currency = currency;
        EmploymentType = employmentType;
        StartDate = startDate;
        ExpirationDate = expirationDate;
        Status = status;
        TemplateVersion = templateVersion;
        DocumentObjectKey = documentObjectKey;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        SentAt = sentAt;
        RespondedAt = respondedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the client payload (422 on failure) and builds an unsaved draft (Id = 0, Version = 1).</summary>
    public static Offer Draft(OfferWrite write, DateOnly today, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        var errors = new ValidationErrors();

        if (write.ApplicationId <= 0)
        {
            errors.Add("applicationId", "applicationId must be a positive identifier.");
        }

        if (write.BaseSalary < 0)
        {
            errors.Add("baseSalary", "baseSalary must be zero or positive.");
        }

        if (write.BonusAmount is < 0)
        {
            errors.Add("bonusAmount", "bonusAmount must be zero or positive.");
        }

        if (write.AllowanceAmount is < 0)
        {
            errors.Add("allowanceAmount", "allowanceAmount must be zero or positive.");
        }

        var currency = string.IsNullOrWhiteSpace(write.Currency) ? DefaultCurrency : write.Currency.Trim();
        if (!CurrencyPattern().IsMatch(currency))
        {
            errors.Add("currency", "currency must be a three-letter upper-case ISO 4217 code, for example VND.");
        }

        if (!EmploymentTypeValues.IsValid(write.EmploymentType))
        {
            errors.Add("employmentType", "employmentType must be one of full_time, part_time, hybrid, remote, internship, service_contract.");
        }

        if (write.StartDate == default)
        {
            errors.Add("startDate", "startDate is required.");
        }

        if (write.ExpirationDate == default)
        {
            errors.Add("expirationDate", "expirationDate is required.");
        }
        else
        {
            ValidateExpiration(write.ExpirationDate, write.StartDate, today, errors);
        }

        var templateVersion = write.TemplateVersion?.Trim();
        if (string.IsNullOrEmpty(templateVersion))
        {
            errors.Add("templateVersion", "templateVersion is required.");
        }
        else if (templateVersion.Length > TemplateVersionMaxLength)
        {
            errors.Add("templateVersion", $"templateVersion must be at most {TemplateVersionMaxLength} characters.");
        }

        errors.ThrowIfAny();

        return new Offer(
            id: 0,
            write.ApplicationId,
            write.BaseSalary,
            write.BonusAmount,
            write.AllowanceAmount,
            currency,
            write.EmploymentType!,
            write.StartDate,
            write.ExpirationDate,
            OfferStatus.Draft,
            templateVersion!,
            documentObjectKey: null,
            approvedBy: null,
            approvedAt: null,
            sentAt: null,
            respondedAt: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>draft → approved (HR Manager).</summary>
    public void Approve(long approverUserId, DateTimeOffset now)
    {
        RequireStatus(OfferAction.Approve.ToContract(), OfferStatus.Draft);
        Status = OfferStatus.Approved;
        ApprovedBy = approverUserId;
        ApprovedAt = now;
        Touch(now);
    }

    /// <summary>approved → sent; the caller issues the candidate response token and the outbox e-mail.</summary>
    public void Send(DateTimeOffset now)
    {
        RequireStatus(OfferAction.Send.ToContract(), OfferStatus.Approved);
        Status = OfferStatus.Sent;
        SentAt = now;
        Touch(now);
    }

    /// <summary>sent | expired → sent with a later expiration date (re-approval, REC-06).</summary>
    public void Extend(DateOnly? newExpirationDate, DateOnly today, DateTimeOffset now)
    {
        RequireStatus(OfferAction.Extend.ToContract(), OfferStatus.Sent, OfferStatus.Expired);

        var errors = new ValidationErrors();
        if (newExpirationDate is null)
        {
            errors.Add("expirationDate", "expirationDate is required to extend an offer.");
        }
        else
        {
            ValidateExpiration(newExpirationDate.Value, StartDate, today, errors);
        }

        errors.ThrowIfAny();

        ExpirationDate = newExpirationDate!.Value;
        Status = OfferStatus.Sent;
        Touch(now);
    }

    /// <summary>draft | approved | sent → cancelled with a mandatory reason.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        var trimmed = reason?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to cancel an offer.");
        }

        if (trimmed.Length > ReasonMaxLength)
        {
            throw CoreHrValidationException.For("reason", $"reason must be at most {ReasonMaxLength} characters.");
        }

        RequireStatus(OfferAction.Cancel.ToContract(), OfferStatus.Draft, OfferStatus.Approved, OfferStatus.Sent);
        Status = OfferStatus.Cancelled;
        Touch(now);
    }

    /// <summary>sent → expired (lazy expiry or worker).</summary>
    public void Expire(DateTimeOffset now)
    {
        RequireStatus("expire", OfferStatus.Sent);
        Status = OfferStatus.Expired;
        Touch(now);
    }

    /// <summary>sent → accepted (candidate).</summary>
    public void Accept(DateTimeOffset now)
    {
        RequireStatus(OfferDecision.Accept.ToContract(), OfferStatus.Sent);
        Status = OfferStatus.Accepted;
        RespondedAt = now;
        Touch(now);
    }

    /// <summary>sent → declined (candidate).</summary>
    public void Decline(DateTimeOffset now)
    {
        RequireStatus(OfferDecision.Decline.ToContract(), OfferStatus.Sent);
        Status = OfferStatus.Declined;
        RespondedAt = now;
        Touch(now);
    }

    /// <summary>True when the offer is still <c>sent</c> but its expiration date lies strictly before <paramref name="today"/>.</summary>
    public bool IsExpired(DateOnly today) => Status == OfferStatus.Sent && ExpirationDate < today;

    /// <summary>True when the candidate may still accept or decline.</summary>
    public bool IsOpenForResponse(DateOnly today) => Status == OfferStatus.Sent && !IsExpired(today);

    public OfferSnapshot Snapshot() => new(Status, ExpirationDate, Version);

    private static void ValidateExpiration(DateOnly expirationDate, DateOnly startDate, DateOnly today, ValidationErrors errors)
    {
        if (expirationDate < today)
        {
            errors.Add("expirationDate", "expirationDate must not be in the past.");
        }

        if (startDate != default && expirationDate > startDate)
        {
            errors.Add("expirationDate", "expirationDate must be on or before startDate.");
        }
    }

    private void RequireStatus(string action, params OfferStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw new CoreHrBusinessRuleException(
                InvalidTransitionCode,
                $"Cannot {action} an offer in status {Status.ToContract()}. Allowed from: {string.Join(", ", allowed.Select(status => status.ToContract()))}.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["currentStatus"] = Status.ToContract(),
                    ["action"] = action
                }
            };
        }
    }

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();
}
