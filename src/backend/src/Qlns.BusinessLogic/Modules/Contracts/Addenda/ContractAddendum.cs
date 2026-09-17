using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>
/// Contract addendum (table <c>contract_addenda</c>, CON-03). Never modifies the original contract: it records the
/// terms before and after the change and, once effective, raises an employee event for master-data changes.
/// Workflow: draft → pending_approval → approved → (signed) → effective; effective addenda that change the same
/// terms as a newer one become superseded. Every successful mutation bumps <see cref="Version"/> (the ETag).
/// </summary>
public sealed class ContractAddendum
{
    public const int AddendumNumberMaxLength = 100;
    public const int ReasonMaxLength = 5000;

    public const string InvalidTransitionCode = "contracts.addendum.invalid_transition";
    public const string SignatureRequiredCode = "contracts.addendum.signature_required";
    public const string SignedDocumentNotAllowedCode = "contracts.addendum.signed_document_not_allowed";

    public long Id { get; }
    public long ContractId { get; }
    public string AddendumNumber { get; }
    public ContractAddendumStatus Status { get; private set; }
    public DateOnly EffectiveDate { get; }
    public JsonObject BeforeTerms { get; }
    public JsonObject AfterTerms { get; }
    public string Reason { get; }
    public string? DocumentObjectKey { get; private set; }
    public long CreatedBy { get; }
    public long? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public bool SignedDocumentAvailable => DocumentObjectKey is not null;

    /// <summary>Names of the terms this addendum changes (afterTerms property names).</summary>
    public IReadOnlyList<string> ChangedTerms => AfterTerms.Select(pair => pair.Key).ToList();

    public ContractAddendum(
        long id,
        long contractId,
        string addendumNumber,
        ContractAddendumStatus status,
        DateOnly effectiveDate,
        JsonObject beforeTerms,
        JsonObject afterTerms,
        string reason,
        string? documentObjectKey,
        long createdBy,
        long? approvedBy,
        DateTimeOffset? approvedAt,
        DateTimeOffset? signedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0 || (id == 0 && status != ContractAddendumStatus.Draft))
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Only an unsaved draft may have id 0.");
        }

        if (contractId <= 0 || createdBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractId), "Persistent identifiers must be positive.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(addendumNumber);

        Id = id;
        ContractId = contractId;
        AddendumNumber = addendumNumber;
        Status = status;
        EffectiveDate = effectiveDate;
        BeforeTerms = beforeTerms ?? throw new ArgumentNullException(nameof(beforeTerms));
        AfterTerms = afterTerms ?? throw new ArgumentNullException(nameof(afterTerms));
        Reason = reason ?? throw new ArgumentNullException(nameof(reason));
        DocumentObjectKey = documentObjectKey;
        CreatedBy = createdBy;
        ApprovedBy = approvedBy;
        ApprovedAt = approvedAt;
        SignedAt = signedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the payload against the parent contract (422) and builds an unsaved draft (Id = 0, Version = 1).</summary>
    public static ContractAddendum CreateDraft(Contract contract, ContractAddendumWrite write, long createdBy, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(write);
        var errors = new ValidationErrors();

        var addendumNumber = write.AddendumNumber?.Trim();
        if (string.IsNullOrEmpty(addendumNumber))
        {
            errors.Add("addendumNumber", "addendumNumber is required.");
        }
        else if (addendumNumber.Length > AddendumNumberMaxLength)
        {
            errors.Add("addendumNumber", $"addendumNumber must be at most {AddendumNumberMaxLength} characters.");
        }

        if (write.EffectiveDate == default)
        {
            errors.Add("effectiveDate", "effectiveDate is required.");
        }
        else if (write.EffectiveDate < contract.StartDate)
        {
            errors.Add("effectiveDate", $"effectiveDate must not be before the contract start date {contract.StartDate:yyyy-MM-dd}.");
        }

        if (write.BeforeTerms is null)
        {
            errors.Add("beforeTerms", "beforeTerms must be a JSON object.");
        }

        if (write.AfterTerms is null)
        {
            errors.Add("afterTerms", "afterTerms must be a JSON object.");
        }
        else
        {
            AddendumTermRules.ValidateAfterTerms(write.AfterTerms, errors);
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

        errors.ThrowIfAny();

        return new ContractAddendum(
            id: 0,
            contract.Id,
            addendumNumber!,
            ContractAddendumStatus.Draft,
            write.EffectiveDate,
            write.BeforeTerms!.DeepClone().AsObject(),
            write.AfterTerms!.DeepClone().AsObject(),
            reason!,
            documentObjectKey: null,
            createdBy,
            approvedBy: null,
            approvedAt: null,
            signedAt: null,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>draft → pending_approval.</summary>
    public void Submit(DateTimeOffset now)
    {
        RequireStatus(ContractAddendumAction.Submit, ContractAddendumStatus.Draft);
        Status = ContractAddendumStatus.PendingApproval;
        Touch(now);
    }

    /// <summary>pending_approval → approved; records the approver.</summary>
    public void Approve(long approverUserId, DateTimeOffset now)
    {
        RequireStatus(ContractAddendumAction.Approve, ContractAddendumStatus.PendingApproval);
        Status = ContractAddendumStatus.Approved;
        ApprovedBy = approverUserId;
        ApprovedAt = now;
        Touch(now);
    }

    /// <summary>Throws 409 unless the addendum is approved, the only status that accepts a signed document.</summary>
    public void RequireSignedDocumentAllowed()
    {
        if (Status != ContractAddendumStatus.Approved)
        {
            throw new CoreHrBusinessRuleException(
                SignedDocumentNotAllowedCode,
                $"A signed document can only be attached to an approved addendum; this addendum is {Status.ToContract()}.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = Status.ToContract() }
            };
        }
    }

    /// <summary>Records the signed PDF of an approved addendum. Signing itself is confirmed by <see cref="MarkSigned"/>.</summary>
    public void AttachSignedDocument(string objectKey, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        RequireSignedDocumentAllowed();
        DocumentObjectKey = objectKey;
        Touch(now);
    }

    /// <summary>Confirms signature of an approved, not yet signed addendum whose signed document is stored (409 otherwise). Status stays approved.</summary>
    public void MarkSigned(DateTimeOffset now)
    {
        RequireStatus(ContractAddendumAction.MarkSigned, ContractAddendumStatus.Approved);

        if (SignedAt is not null)
        {
            throw InvalidTransition(ContractAddendumAction.MarkSigned, "the addendum is already marked as signed");
        }

        if (!SignedDocumentAvailable)
        {
            throw new CoreHrBusinessRuleException(
                SignatureRequiredCode,
                "Upload the signed addendum document before marking the addendum as signed.");
        }

        SignedAt = now;
        Touch(now);
    }

    /// <summary>approved (signed) → effective.</summary>
    public void MakeEffective(DateTimeOffset now)
    {
        RequireStatus(ContractAddendumAction.MakeEffective, ContractAddendumStatus.Approved);

        if (SignedAt is null)
        {
            throw new CoreHrBusinessRuleException(
                SignatureRequiredCode,
                "Only a signed addendum can become effective; upload the signed document and mark it signed first.");
        }

        Status = ContractAddendumStatus.Effective;
        Touch(now);
    }

    /// <summary>effective → superseded, when a newer effective addendum changes at least one of the same terms.</summary>
    public void Supersede(DateTimeOffset now)
    {
        if (Status != ContractAddendumStatus.Effective)
        {
            throw InvalidTransition("supersede");
        }

        Status = ContractAddendumStatus.Superseded;
        Touch(now);
    }

    /// <summary>draft | pending_approval | approved → cancelled with a mandatory reason.</summary>
    public void Cancel(string? reason, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw CoreHrValidationException.For("reason", "A reason is required to cancel an addendum.");
        }

        RequireStatus(
            ContractAddendumAction.Cancel,
            ContractAddendumStatus.Draft,
            ContractAddendumStatus.PendingApproval,
            ContractAddendumStatus.Approved);

        Status = ContractAddendumStatus.Cancelled;
        Touch(now);
    }

    /// <summary>True when both addenda change at least one common term.</summary>
    public bool SharesTermWith(ContractAddendum other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return AfterTerms.Any(pair => other.AfterTerms.ContainsKey(pair.Key));
    }

    private void RequireStatus(ContractAddendumAction action, params ContractAddendumStatus[] allowed)
    {
        if (!allowed.Contains(Status))
        {
            throw InvalidTransition(action.ToContract());
        }
    }

    private CoreHrBusinessRuleException InvalidTransition(ContractAddendumAction action, string because) => new(
        InvalidTransitionCode,
        $"Cannot {action.ToContract()} the addendum: {because}.")
    {
        Details = new Dictionary<string, object?>
        {
            ["currentStatus"] = Status.ToContract(),
            ["action"] = action.ToContract()
        }
    };

    private CoreHrBusinessRuleException InvalidTransition(string action) => new(
        InvalidTransitionCode,
        $"Cannot {action} an addendum in status {Status.ToContract()}. " +
        "Allowed workflow: draft → pending_approval → approved → effective; draft, pending_approval and approved may be cancelled.")
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
}
