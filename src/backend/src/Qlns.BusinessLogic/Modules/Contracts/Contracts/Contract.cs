using System.Text.RegularExpressions;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// Employment contract (table <c>contracts</c>, CON-01). Workflow: draft → approved → executed (signed) → active →
/// expired | terminated; draft and approved may be cancelled. Only drafts are editable. Every successful mutation
/// bumps <see cref="Version"/> and sets <see cref="UpdatedAt"/>. <see cref="DocumentObjectKey"/> is internal storage
/// addressing and never leaves the backend.
/// </summary>
public sealed partial class Contract
{
    public const int ContractNumberMaxLength = 100;
    public const int MaxProbationDays = 60;
    public const string DefaultCurrency = "VND";

    public const string InvalidTransitionCode = "contracts.contract.invalid_transition";
    public const string NotEditableCode = "contracts.contract.not_editable";
    public const string SignatureRequiredCode = "contracts.contract.signature_required";
    public const string SignedDocumentNotAllowedCode = "contracts.contract.signed_document_not_allowed";

    public long Id { get; }
    public long EmployeeId { get; }
    public string ContractNumber { get; private set; }
    public ContractType ContractType { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }
    public decimal Salary { get; private set; }
    public string Currency { get; private set; }
    public int? NoticePeriodDays { get; private set; }
    public ContractStatus Status { get; private set; }
    public bool IsPrimary { get; private set; }
    public string? DocumentObjectKey { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool SignedDocumentAvailable => DocumentObjectKey is not null;

    /// <summary>True while the contract occupies the employee's single primary in-force slot.</summary>
    public bool IsInForce => Status.IsInForce();

    public Contract(
        long id,
        long employeeId,
        string contractNumber,
        ContractType contractType,
        DateOnly startDate,
        DateOnly? endDate,
        decimal salary,
        string currency,
        int? noticePeriodDays,
        ContractStatus status,
        bool isPrimary,
        string? documentObjectKey,
        DateTimeOffset? signedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != ContractStatus.Draft))
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

        ArgumentException.ThrowIfNullOrWhiteSpace(contractNumber);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);

        Id = id;
        EmployeeId = employeeId;
        ContractNumber = contractNumber;
        ContractType = contractType;
        StartDate = startDate;
        EndDate = endDate;
        Salary = salary;
        Currency = currency;
        NoticePeriodDays = noticePeriodDays;
        Status = status;
        IsPrimary = isPrimary;
        DocumentObjectKey = documentObjectKey;
        SignedAt = signedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the client payload (422 on failure) and builds an unsaved draft (Id = 0, Version = 1).</summary>
    public static Contract CreateDraft(ContractWrite write, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);
        var terms = ValidatedTerms.From(write);

        return new Contract(
            id: 0,
            terms.EmployeeId,
            terms.ContractNumber,
            terms.ContractType,
            terms.StartDate,
            terms.EndDate,
            terms.Salary,
            terms.Currency,
            terms.NoticePeriodDays,
            ContractStatus.Draft,
            terms.IsPrimary,
            documentObjectKey: null,
            signedAt: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>
    /// Full replace of a draft (<c>PUT</c>). The employee cannot change (422); non-drafts are not editable (409).
    /// Returns the contract field names whose value changed, for the audit row.
    /// </summary>
    public IReadOnlyList<string> Replace(ContractWrite write, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(write);

        if (Status != ContractStatus.Draft)
        {
            throw new CoreHrBusinessRuleException(
                NotEditableCode,
                $"Only draft contracts can be edited; this contract is {Status.ToContract()}.");
        }

        var errors = new ValidationErrors();
        if (write.EmployeeId != EmployeeId)
        {
            errors.Add("employeeId", "The employee of an existing contract cannot be changed; create a new contract instead.");
        }

        var terms = ValidatedTerms.From(write, errors);

        var changed = new List<string>();
        Set(ref changed, "contractNumber", ContractNumber, terms.ContractNumber, value => ContractNumber = value);
        Set(ref changed, "contractType", ContractType, terms.ContractType, value => ContractType = value);
        Set(ref changed, "startDate", StartDate, terms.StartDate, value => StartDate = value);
        Set(ref changed, "endDate", EndDate, terms.EndDate, value => EndDate = value);
        Set(ref changed, "salary", Salary, terms.Salary, value => Salary = value);
        Set(ref changed, "currency", Currency, terms.Currency, value => Currency = value);
        Set(ref changed, "noticePeriodDays", NoticePeriodDays, terms.NoticePeriodDays, value => NoticePeriodDays = value);
        Set(ref changed, "isPrimary", IsPrimary, terms.IsPrimary, value => IsPrimary = value);

        Touch(now);
        return changed;
    }

    /// <summary>draft → approved.</summary>
    public void Approve(DateTimeOffset now)
    {
        RequireStatus(ContractAction.Approve, ContractStatus.Draft);
        Status = ContractStatus.Approved;
        Touch(now);
    }

    /// <summary>
    /// Records the signed PDF (approved or executed only, 409 otherwise) and the signing instant when not yet
    /// known. An approved contract becomes executed unless <paramref name="otherPrimaryInForce"/> reports that the
    /// employee's current primary contract still occupies the in-force slot (ux_contracts_primary_active): the
    /// contract then keeps the document and stays approved until activation supersedes the old one.
    /// </summary>
    public void AttachSignedDocument(string objectKey, bool otherPrimaryInForce, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        RequireSignedDocumentAllowed();

        DocumentObjectKey = objectKey;
        SignedAt ??= now;

        if (Status == ContractStatus.Approved && !(IsPrimary && otherPrimaryInForce))
        {
            Status = ContractStatus.Executed;
        }

        Touch(now);
    }

    /// <summary>Throws 409 unless the contract is approved or executed, the only statuses that accept a signed document.</summary>
    public void RequireSignedDocumentAllowed()
    {
        if (Status is not (ContractStatus.Approved or ContractStatus.Executed))
        {
            throw new CoreHrBusinessRuleException(
                SignedDocumentNotAllowedCode,
                $"A signed document can only be attached to an approved or executed contract; this contract is {Status.ToContract()}.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = Status.ToContract() }
            };
        }
    }

    /// <summary>
    /// approved | executed → active. Signature evidence is mandatory: a stored signed document or an explicit
    /// <paramref name="signedAt"/> (409 otherwise). An explicit instant is recorded as the signing time.
    /// </summary>
    public void Activate(DateTimeOffset? signedAt, DateTimeOffset now)
    {
        RequireStatus(ContractAction.Activate, ContractStatus.Approved, ContractStatus.Executed);

        if (!SignedDocumentAvailable && signedAt is null)
        {
            throw new CoreHrBusinessRuleException(
                SignatureRequiredCode,
                "Activation requires signature evidence: upload the signed document or supply signedAt.");
        }

        if (signedAt is { } explicitSignedAt)
        {
            SignedAt = explicitSignedAt;
        }

        Status = ContractStatus.Active;
        Touch(now);
    }

    /// <summary>active | executed → terminated with a mandatory reason.</summary>
    public void Terminate(string? reason, DateTimeOffset now)
    {
        RequireReason(reason, "terminate");
        RequireStatus(ContractAction.Terminate, ContractStatus.Active, ContractStatus.Executed);
        Status = ContractStatus.Terminated;
        Touch(now);
    }

    /// <summary>draft | approved → cancelled with a mandatory reason.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        RequireReason(reason, "cancel");
        RequireStatus(ContractAction.Cancel, ContractStatus.Draft, ContractStatus.Approved);
        Status = ContractStatus.Cancelled;
        Touch(now);
    }

    /// <summary>
    /// active | executed → expired, for a contract whose end date lies before <paramref name="asOf"/>
    /// (the day of the background scan, or the start date of the successor contract).
    /// </summary>
    public void Expire(DateOnly asOf, DateTimeOffset now)
    {
        if (!Status.IsInForce())
        {
            throw InvalidTransition("expire");
        }

        if (EndDate is not { } endDate || endDate >= asOf)
        {
            throw new CoreHrBusinessRuleException(
                InvalidTransitionCode,
                $"Contract {Id} has not ended by {asOf:yyyy-MM-dd} and cannot be expired.");
        }

        Status = ContractStatus.Expired;
        Touch(now);
    }

    /// <summary>Days from <paramref name="asOf"/> to the end date (negative once ended); null for indefinite contracts.</summary>
    public int? DaysRemaining(DateOnly asOf) => EndDate is { } endDate ? endDate.DayNumber - asOf.DayNumber : null;

    /// <summary>True when this in-force contract's end date lies strictly before <paramref name="date"/>.</summary>
    public bool EndsBefore(DateOnly date) => EndDate is { } endDate && endDate < date;

    private static void Set<T>(ref List<string> changed, string field, T current, T next, Action<T> apply)
    {
        if (!EqualityComparer<T>.Default.Equals(current, next))
        {
            apply(next);
            changed.Add(field);
        }
    }

    private static void RequireReason(string? reason, string action)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", $"A reason is required to {action} a contract.");
        }
    }

    private void RequireStatus(ContractAction action, params ContractStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw InvalidTransition(action.ToContract());
        }
    }

    private CoreHrBusinessRuleException InvalidTransition(string action) => new(
        InvalidTransitionCode,
        $"Cannot {action} a contract in status {Status.ToContract()}. " +
        "Allowed workflow: draft → approved → executed → active → expired | terminated; draft and approved may be cancelled.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action
        }
    };

    private void Touch(DateTimeOffset now)
    {
        Version++;
        UpdatedAt = now;
    }

    /// <summary>Normalised, validated ContractWrite. Collects every field error before throwing (422).</summary>
    private sealed record ValidatedTerms(
        long EmployeeId,
        string ContractNumber,
        ContractType ContractType,
        DateOnly StartDate,
        DateOnly? EndDate,
        decimal Salary,
        string Currency,
        int? NoticePeriodDays,
        bool IsPrimary)
    {
        public static ValidatedTerms From(ContractWrite write) => From(write, new ValidationErrors());

        public static ValidatedTerms From(ContractWrite write, ValidationErrors errors)
        {
            if (write.EmployeeId <= 0)
            {
                errors.Add("employeeId", "employeeId must be a positive identifier.");
            }

            var contractNumber = write.ContractNumber?.Trim();
            if (string.IsNullOrEmpty(contractNumber))
            {
                errors.Add("contractNumber", "contractNumber is required.");
            }
            else if (contractNumber.Length > ContractNumberMaxLength)
            {
                errors.Add("contractNumber", $"contractNumber must be at most {ContractNumberMaxLength} characters.");
            }

            var typeKnown = ContractTypeNames.TryParseContract(write.ContractType, out var contractType);
            if (!typeKnown)
            {
                errors.Add("contractType", "contractType must be one of probation, fixed_term, indefinite, internship, service_contract.");
            }

            if (write.StartDate == default)
            {
                errors.Add("startDate", "startDate is required.");
            }

            if (typeKnown)
            {
                ValidateTerm(contractType, write.StartDate, write.EndDate, errors);
            }

            if (write.Salary < 0)
            {
                errors.Add("salary", "salary must be zero or positive.");
            }

            var currency = string.IsNullOrWhiteSpace(write.Currency) ? DefaultCurrency : write.Currency.Trim();
            if (!CurrencyPattern().IsMatch(currency))
            {
                errors.Add("currency", "currency must be a three-letter ISO 4217 code in upper case, for example VND.");
            }

            if (write.NoticePeriodDays is < 0)
            {
                errors.Add("noticePeriodDays", "noticePeriodDays must be zero or positive.");
            }

            errors.ThrowIfAny();

            return new ValidatedTerms(
                write.EmployeeId,
                contractNumber!,
                contractType,
                write.StartDate,
                write.EndDate,
                write.Salary,
                currency,
                write.NoticePeriodDays,
                write.IsPrimary);
        }

        private static void ValidateTerm(ContractType type, DateOnly startDate, DateOnly? endDate, ValidationErrors errors)
        {
            if (!type.HasFixedTerm())
            {
                if (endDate is not null)
                {
                    errors.Add("endDate", "An indefinite contract must not have an end date.");
                }

                return;
            }

            if (endDate is not { } end)
            {
                errors.Add("endDate", "A fixed-term contract must have an end date later than its start date.");
                return;
            }

            if (end <= startDate)
            {
                errors.Add("endDate", "A fixed-term contract must have an end date later than its start date.");
                return;
            }

            if (type == ContractType.Probation && end.DayNumber - startDate.DayNumber > MaxProbationDays)
            {
                errors.Add("endDate", $"A probation contract may last at most {MaxProbationDays} days (Labour Code: 30 or 60 days).");
            }
        }
    }

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();
}
