using System.Globalization;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// CON-01 / CON-02 use cases: search and read contracts within data scope, draft and edit, run the
/// approve → activate → terminate | cancel workflow with the primary-contract overlap rule, attach the signed PDF,
/// issue signed download links, list expiring contracts and expire due contracts for the background worker.
/// Out-of-scope contracts are reported as not found; permission failures as forbidden.
/// </summary>
public sealed class ContractService(
    IContractRepository repository,
    SignedDocumentUploader uploader,
    IDocumentStorage storage,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan SignedUrlLifetime = TimeSpan.FromMinutes(15);

    public const string ResourceName = "Contract";
    public const string DocumentResourceName = "Signed contract document";
    private const string ConflictResource = "contract";

    public const string WriteForbiddenCode = "contracts.contract.write_forbidden";
    public const string ApproveForbiddenCode = "contracts.contract.approve_forbidden";
    public const string ExpiringForbiddenCode = "contracts.contract.expiring_forbidden";
    public const string OverlapOverrideForbiddenCode = "contracts.contract.overlap_override_forbidden";
    public const string NumberTakenCode = "contracts.contract.number_taken";
    public const string PrimaryOverlapCode = "contracts.contract.primary_overlap";
    public const string ScanUnavailableCode = "contracts.contract.scan_unavailable";

    public Task<PagedResult<Contract>> SearchAsync(ContractSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);
        return repository.SearchAsync(query, actor, cancellationToken);
    }

    public async Task<Contract> GetAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return await repository.GetByIdAsync(contractId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, contractId);
    }

    public async Task<Contract> CreateAsync(CreateContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        // Shape and rule validation is pure and leaks nothing, so it runs before the scoped employee lookup.
        var draft = Contract.CreateDraft(command.Write, timeProvider.GetUtcNow());

        await RequireVisibleEmployeeAsync(draft.EmployeeId, actor, cancellationToken);
        RequireWrite(actor);
        await RequireUniqueNumberAsync(draft.ContractNumber, excludeContractId: null, cancellationToken);

        return await repository.InsertAsync(draft, actor, cancellationToken);
    }

    public async Task<Contract> ReplaceAsync(ReplaceContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var contract = await LoadAsync(command.ContractId, command.ExpectedVersion, actor, cancellationToken);
        RequireWrite(actor);

        var changedFields = contract.Replace(command.Write, timeProvider.GetUtcNow());
        await RequireUniqueNumberAsync(contract.ContractNumber, contract.Id, cancellationToken);

        var saved = await repository.SaveReplacementAsync(contract, command.ExpectedVersion, changedFields, actor, cancellationToken);
        return saved ? contract : throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    public async Task<Contract> TransitionAsync(TransitionContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;
        var options = command.Options ?? ContractActionOptions.None;

        var contract = await LoadAsync(command.ContractId, command.ExpectedVersion, actor, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var previousStatus = contract.Status;
        var reason = options.Reason?.Trim();

        switch (command.Action)
        {
            case ContractAction.Approve:
                RequireApprove(actor);
                contract.Approve(now);
                break;
            case ContractAction.Activate:
                RequireWrite(actor);
                return await ActivateAsync(contract, command.ExpectedVersion, options, actor, now, cancellationToken);
            case ContractAction.Terminate:
                RequireWrite(actor);
                contract.Terminate(reason, now);
                break;
            case ContractAction.Cancel:
                RequireWrite(actor);
                contract.Cancel(reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unknown contract action.");
        }

        var saved = await repository.SaveTransitionAsync(
            contract,
            previousStatus,
            command.ExpectedVersion,
            command.Action == ContractAction.Approve ? null : reason,
            actor,
            cancellationToken);

        return saved ? contract : throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    public async Task<Contract> AttachSignedDocumentAsync(UploadSignedContractCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var contract = await LoadAsync(command.ContractId, command.ExpectedVersion, actor, cancellationToken);
        RequireWrite(actor);

        // Fail before touching storage; the domain repeats the check when the document is attached.
        contract.RequireSignedDocumentAllowed();

        var otherPrimaryInForce = contract.IsPrimary &&
            await repository.FindOtherPrimaryInForceAsync(contract.EmployeeId, contract.Id, cancellationToken) is not null;

        var objectKey = await uploader.ScanAndStoreAsync(
            BuildSignedObjectKey(contract.Id),
            command.Upload,
            ScanUnavailableCode,
            cancellationToken);

        var previousStatus = contract.Status;
        contract.AttachSignedDocument(objectKey, otherPrimaryInForce, timeProvider.GetUtcNow());

        bool saved;
        try
        {
            saved = await repository.SaveSignedDocumentAsync(contract, previousStatus, command.ExpectedVersion, actor, cancellationToken);
        }
        catch
        {
            await uploader.TryDeleteAsync(objectKey);
            throw;
        }

        if (!saved)
        {
            await uploader.TryDeleteAsync(objectKey);
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return contract;
    }

    public async Task<SignedDownload> CreateDownloadUrlAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var contract = await GetAsync(contractId, actor, cancellationToken);
        if (contract.DocumentObjectKey is not { } objectKey)
        {
            throw new CoreHrNotFoundException(DocumentResourceName, contractId);
        }

        var now = timeProvider.GetUtcNow();
        var expiresAt = now + SignedUrlLifetime;
        var url = await storage.CreateSignedDownloadUrlAsync(
            objectKey,
            DownloadFileName(contract.Id),
            expiresAt,
            cancellationToken);

        await repository.RecordDownloadAsync(contract, actor, now, expiresAt, cancellationToken);
        return new SignedDownload(url, expiresAt);
    }

    /// <summary>
    /// CON-02.1: in-force contracts within scope that have reached the amber threshold of their type, at most
    /// <see cref="ExpiringContractsQuery.WithinDays"/> days ahead of <see cref="ExpiringContractsQuery.AsOf"/>.
    /// Self-only actors have no dashboard and are refused.
    /// </summary>
    public async Task<ExpiringContractsView> ListExpiringAsync(ExpiringContractsQuery query, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);

        if (actor.DataScope.IsSelfOnly)
        {
            throw new CoreHrForbiddenException(
                ExpiringForbiddenCode,
                "The expiring-contracts list is available to HR and managers with a department or organization scope.");
        }

        if (query.WithinDays is < 1 or > ExpiryAlertPolicy.MaxWithinDays)
        {
            throw CoreHrValidationException.For("withinDays", $"withinDays must be between 1 and {ExpiryAlertPolicy.MaxWithinDays}.");
        }

        var asOf = query.AsOf ?? Today();
        var windows = ExpiryAlertPolicy.Windows(asOf, query.WithinDays);
        var page = await repository.SearchExpiringAsync(asOf, windows, actor, query.Page, cancellationToken);

        return new ExpiringContractsView(page.Map(contract => ToExpiring(contract, asOf)), asOf);
    }

    /// <summary>
    /// Background worker entry point: every active contract whose end date passed before <paramref name="today"/>
    /// becomes expired, audited under <paramref name="actor"/>. Version races are reported, not thrown.
    /// </summary>
    public async Task<ExpireDueContractsResult> ExpireDueContractsAsync(DateOnly today, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var due = await repository.ListDueForExpiryAsync(today, cancellationToken);
        var expired = 0;
        var conflicted = new List<long>();

        foreach (var contract in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var previousStatus = contract.Status;
            var expectedVersion = contract.Version;
            contract.Expire(today, timeProvider.GetUtcNow());

            var saved = await repository.SaveTransitionAsync(contract, previousStatus, expectedVersion, reason: null, actor, cancellationToken);
            if (saved)
            {
                expired++;
            }
            else
            {
                conflicted.Add(contract.Id);
            }
        }

        return new ExpireDueContractsResult(expired, conflicted);
    }

    private async Task<Contract> ActivateAsync(
        Contract contract,
        long expectedVersion,
        ContractActionOptions options,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var previousStatus = contract.Status;
        contract.Activate(options.SignedAt, now);

        var superseded = contract.IsPrimary
            ? await ResolvePrimaryOverlapAsync(contract, options, actor, now, cancellationToken)
            : null;

        ProbationReviewDraft? probationReview = null;
        if (contract.ContractType == ContractType.Probation)
        {
            var employee = await repository.GetEmployeeAsync(contract.EmployeeId, cancellationToken)
                ?? throw new CoreHrNotFoundException("Employee", contract.EmployeeId);
            probationReview = ProbationReviewDraft.For(contract, employee);
        }

        var saved = await repository.SaveActivationAsync(
            new ContractActivation(contract, previousStatus, expectedVersion, superseded, probationReview),
            actor,
            cancellationToken);

        return saved ? contract : throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    /// <summary>
    /// CON-01 rule: one primary in-force contract per employee. A predecessor that ends before this contract
    /// starts expires by natural succession; otherwise the activation is refused unless an approver explicitly
    /// allows the overlap with a reason, which terminates the predecessor.
    /// </summary>
    private async Task<SupersededContract?> ResolvePrimaryOverlapAsync(
        Contract contract,
        ContractActionOptions options,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var other = await repository.FindOtherPrimaryInForceAsync(contract.EmployeeId, contract.Id, cancellationToken);
        if (other is null)
        {
            return null;
        }

        var previousStatus = other.Status;
        var expectedVersion = other.Version;

        if (other.EndsBefore(contract.StartDate))
        {
            other.Expire(contract.StartDate, now);
            return new SupersededContract(other, previousStatus, expectedVersion, OverrideReason: null);
        }

        if (!options.AllowPrimaryOverlap)
        {
            throw new CoreHrBusinessRuleException(
                PrimaryOverlapCode,
                $"Employee {contract.EmployeeId} already has primary contract {other.Id} in force ({other.Status.ToContract()}). " +
                "End it first, or let an approver activate with allowPrimaryOverlap and a reason.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["overlappingContractId"] = other.Id,
                    ["overlappingContractStatus"] = other.Status.ToContract()
                }
            };
        }

        if (!actor.HasPermission(ContractPermissions.Approve))
        {
            throw new CoreHrForbiddenException(
                OverlapOverrideForbiddenCode,
                "Overriding the primary-contract overlap rule requires the contracts.contract.approve permission.");
        }

        var reason = options.Reason?.Trim();
        if (string.IsNullOrEmpty(reason))
        {
            throw CoreHrValidationException.For("reason", "A reason is required when allowing a primary-contract overlap.");
        }

        other.Terminate(reason, now);
        return new SupersededContract(other, previousStatus, expectedVersion, reason);
    }

    private async Task<Contract> LoadAsync(long contractId, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var contract = await GetAsync(contractId, actor, cancellationToken);
        if (contract.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return contract;
    }

    private async Task<ContractEmployee> RequireVisibleEmployeeAsync(long employeeId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var employee = await repository.GetEmployeeAsync(employeeId, cancellationToken);
        if (employee is null || !actor.CanAccessEmployee(employeeId, employee.DepartmentId))
        {
            throw new CoreHrNotFoundException("Employee", employeeId);
        }

        return employee;
    }

    private async Task RequireUniqueNumberAsync(string contractNumber, long? excludeContractId, CancellationToken cancellationToken)
    {
        if (await repository.ContractNumberExistsAsync(contractNumber, excludeContractId, cancellationToken))
        {
            throw NumberTaken(contractNumber);
        }
    }

    public static CoreHrBusinessRuleException NumberTaken(string contractNumber) => new(
        NumberTakenCode,
        $"Contract number '{contractNumber}' is already used by another contract.")
    {
        Details = new Dictionary<string, object?> { ["contractNumber"] = contractNumber }
    };

    private static void RequireWrite(CoreHrActor actor)
    {
        if (actor.DataScope.IsSelfOnly || !actor.HasPermission(ContractPermissions.Write))
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Drafting, editing, activating or ending contracts requires the contracts.contract.write permission and an HR data scope.");
        }
    }

    private static void RequireApprove(CoreHrActor actor)
    {
        if (actor.DataScope.IsSelfOnly || !actor.HasPermission(ContractPermissions.Approve))
        {
            throw new CoreHrForbiddenException(
                ApproveForbiddenCode,
                "Approving contracts requires the contracts.contract.approve permission.");
        }
    }

    private static ExpiringContract ToExpiring(Contract contract, DateOnly asOf)
    {
        var daysRemaining = contract.DaysRemaining(asOf)
            ?? throw new InvalidOperationException($"Contract {contract.Id} has no end date and cannot be expiring.");
        var level = ExpiryAlertPolicy.Evaluate(contract.ContractType, daysRemaining)
            ?? throw new InvalidOperationException($"Contract {contract.Id} is outside its alert window ({daysRemaining} days remaining).");

        return new ExpiringContract(contract, daysRemaining, level);
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);

    private static string BuildSignedObjectKey(long contractId) =>
        string.Create(CultureInfo.InvariantCulture, $"contracts/{contractId}/signed/{Guid.NewGuid():N}");

    /// <summary>Download name built from the id only: contract numbers are free text and may contain path characters.</summary>
    private static string DownloadFileName(long contractId) =>
        string.Create(CultureInfo.InvariantCulture, $"contract-{contractId}-signed.pdf");
}
