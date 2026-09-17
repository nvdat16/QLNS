using System.Globalization;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>
/// CON-03.1 use cases: list and draft addenda of a visible in-force contract, run the
/// submit → approve → mark-signed → make-effective | cancel workflow and attach the signed PDF. Making an addendum
/// effective supersedes older effective addenda on the same terms and raises an approved employee event for
/// master-data changes; the original contract row is never modified.
/// </summary>
public sealed class ContractAddendumService(
    IContractRepository contracts,
    IContractAddendumRepository addenda,
    SignedDocumentUploader uploader,
    TimeProvider timeProvider)
{
    public const string ResourceName = "Contract addendum";
    private const string ConflictResource = "contract addendum";

    public const string WriteForbiddenCode = "contracts.addendum.write_forbidden";
    public const string ApproveForbiddenCode = "contracts.addendum.approve_forbidden";
    public const string NumberTakenCode = "contracts.addendum.number_taken";
    public const string ContractNotActiveCode = "contracts.addendum.contract_not_active";
    public const string ScanUnavailableCode = "contracts.addendum.scan_unavailable";

    public async Task<IReadOnlyList<ContractAddendum>> ListAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        await RequireVisibleContractAsync(contractId, actor, cancellationToken);
        return await addenda.ListByContractAsync(contractId, cancellationToken);
    }

    public async Task<ContractAddendum> CreateAsync(CreateContractAddendumCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var contract = await RequireVisibleContractAsync(command.ContractId, actor, cancellationToken);
        RequireWrite(actor);
        RequireInForce(contract);

        var draft = ContractAddendum.CreateDraft(contract, command.Write, actor.UserId, timeProvider.GetUtcNow());

        if (await addenda.AddendumNumberExistsAsync(draft.AddendumNumber, cancellationToken))
        {
            throw NumberTaken(draft.AddendumNumber);
        }

        return await addenda.InsertAsync(draft, actor, cancellationToken);
    }

    public async Task<ContractAddendum> TransitionAsync(TransitionContractAddendumCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var (addendum, contract) = await LoadAsync(command.AddendumId, command.ExpectedVersion, actor, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var previousStatus = addendum.Status;
        var reason = command.Reason?.Trim();

        switch (command.Action)
        {
            case ContractAddendumAction.Submit:
                RequireWrite(actor);
                addendum.Submit(now);
                break;
            case ContractAddendumAction.Approve:
                RequireApprove(actor);
                addendum.Approve(actor.UserId, now);
                break;
            case ContractAddendumAction.MarkSigned:
                RequireWrite(actor);
                addendum.MarkSigned(now);
                break;
            case ContractAddendumAction.MakeEffective:
                RequireWrite(actor);
                return await MakeEffectiveAsync(addendum, contract, command.ExpectedVersion, actor, now, cancellationToken);
            case ContractAddendumAction.Cancel:
                RequireWrite(actor);
                addendum.Cancel(reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unknown addendum action.");
        }

        var saved = await addenda.SaveTransitionAsync(
            addendum,
            previousStatus,
            command.ExpectedVersion,
            command.Action == ContractAddendumAction.Cancel ? reason : null,
            actor,
            cancellationToken);

        return saved ? addendum : throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    public async Task<ContractAddendum> AttachSignedDocumentAsync(UploadSignedAddendumCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var (addendum, _) = await LoadAsync(command.AddendumId, command.ExpectedVersion, actor, cancellationToken);
        RequireWrite(actor);

        // Fail before touching storage; the domain repeats the check when the document is attached.
        addendum.RequireSignedDocumentAllowed();

        var objectKey = await uploader.ScanAndStoreAsync(
            BuildSignedObjectKey(addendum.ContractId, addendum.Id),
            command.Upload,
            ScanUnavailableCode,
            cancellationToken);

        addendum.AttachSignedDocument(objectKey, timeProvider.GetUtcNow());

        bool saved;
        try
        {
            saved = await addenda.SaveSignedDocumentAsync(addendum, command.ExpectedVersion, actor, cancellationToken);
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

        return addendum;
    }

    private async Task<ContractAddendum> MakeEffectiveAsync(
        ContractAddendum addendum,
        Contract contract,
        long expectedVersion,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RequireInForce(contract);
        addendum.MakeEffective(now);

        var superseded = new List<SupersededAddendum>();
        foreach (var other in await addenda.ListByContractAsync(addendum.ContractId, cancellationToken))
        {
            if (other.Id != addendum.Id && other.Status == ContractAddendumStatus.Effective && other.SharesTermWith(addendum))
            {
                var otherExpectedVersion = other.Version;
                other.Supersede(now);
                superseded.Add(new SupersededAddendum(other, otherExpectedVersion));
            }
        }

        var employeeEvent = MasterDataEventDraft.From(addendum, contract.EmployeeId);

        var saved = await addenda.SaveEffectiveAsync(
            new AddendumActivation(addendum, expectedVersion, superseded, employeeEvent),
            actor,
            cancellationToken);

        return saved ? addendum : throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    /// <summary>Loads the addendum and its contract; either missing or the contract outside scope ⇒ 404 for the addendum.</summary>
    private async Task<(ContractAddendum Addendum, Contract Contract)> LoadAsync(
        long addendumId,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var addendum = await addenda.GetByIdAsync(addendumId, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, addendumId);

        var contract = await contracts.GetByIdAsync(addendum.ContractId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, addendumId);

        if (addendum.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return (addendum, contract);
    }

    private async Task<Contract> RequireVisibleContractAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken) =>
        await contracts.GetByIdAsync(contractId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ContractService.ResourceName, contractId);

    private static void RequireInForce(Contract contract)
    {
        if (!contract.IsInForce)
        {
            throw new CoreHrBusinessRuleException(
                ContractNotActiveCode,
                $"Addenda require an active or executed contract; contract {contract.Id} is {contract.Status.ToContract()}.")
            {
                Details = new Dictionary<string, object?> { ["contractStatus"] = contract.Status.ToContract() }
            };
        }
    }

    public static CoreHrBusinessRuleException NumberTaken(string addendumNumber) => new(
        NumberTakenCode,
        $"Addendum number '{addendumNumber}' is already used by another addendum.")
    {
        Details = new Dictionary<string, object?> { ["addendumNumber"] = addendumNumber }
    };

    private static void RequireWrite(CoreHrActor actor)
    {
        if (actor.DataScope.IsSelfOnly || !actor.HasPermission(ContractPermissions.Write))
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Drafting, signing or making addenda effective requires the contracts.contract.write permission and an HR data scope.");
        }
    }

    private static void RequireApprove(CoreHrActor actor)
    {
        if (actor.DataScope.IsSelfOnly || !actor.HasPermission(ContractPermissions.Approve))
        {
            throw new CoreHrForbiddenException(
                ApproveForbiddenCode,
                "Approving addenda requires the contracts.contract.approve permission.");
        }
    }

    private static string BuildSignedObjectKey(long contractId, long addendumId) =>
        string.Create(CultureInfo.InvariantCulture, $"contracts/{contractId}/addenda/{addendumId}/{Guid.NewGuid():N}");
}
