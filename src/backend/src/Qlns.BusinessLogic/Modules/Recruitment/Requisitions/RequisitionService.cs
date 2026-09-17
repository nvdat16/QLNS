using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>
/// Use cases of REC-01.1/REC-01.2: search and read requisitions in the actor's data scope, create and edit drafts,
/// and run the submit/approve/reject/publish/close/cancel workflow. Data scope is enforced by the repository on reads
/// (404 outside scope) and here on writes (a department-scoped actor may only target its own departments, 403).
/// Action-level permissions are enforced here; the endpoint policy only requires a coarse claim.
/// </summary>
public sealed class RequisitionService(
    IRequisitionRepository repository,
    TimeProvider timeProvider)
{
    private const string ResourceName = "Requisition";
    private const string ConflictResource = "requisition";

    public const string DepartmentOutOfScopeCode = "recruitment.requisition.department_out_of_scope";
    public const string WriteForbiddenCode = "recruitment.requisition.write_forbidden";
    public const string ApproveForbiddenCode = "recruitment.requisition.approve_forbidden";
    public const string PublishForbiddenCode = "recruitment.requisition.publish_forbidden";
    public const string CancelForbiddenCode = "recruitment.requisition.cancel_forbidden";
    public const string OpenOffersCode = "recruitment.requisition.open_offers";

    public Task<PagedResult<Requisition>> SearchAsync(
        RequisitionSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);
        return repository.SearchAsync(query, actor, cancellationToken);
    }

    public async Task<Requisition> GetAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return await repository.GetByIdAsync(requisitionId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, requisitionId);
    }

    public async Task<Requisition> CreateAsync(CreateRequisitionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        if (!actor.HasPermission(RequisitionPermissions.Write))
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Creating requisitions requires the recruitment.requisition.write permission.");
        }

        var now = timeProvider.GetUtcNow();
        var draft = Requisition.CreateDraft(command.Write, actor.UserId, Today(now), now);

        RequireDepartmentInScope(actor, draft.DepartmentId);
        await RequireReferencesAsync(draft, cancellationToken);

        return await repository.InsertAsync(draft, actor, cancellationToken);
    }

    public async Task<Requisition> ReplaceAsync(ReplaceRequisitionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var requisition = await LoadAsync(command.RequisitionId, command.ExpectedVersion, actor, cancellationToken);
        RequireWriteOrCreator(actor, requisition);

        var now = timeProvider.GetUtcNow();
        var changedFields = requisition.Replace(command.Write, Today(now), now);

        RequireDepartmentInScope(actor, requisition.DepartmentId);
        await RequireReferencesAsync(requisition, cancellationToken);

        var saved = await repository.ReplaceAsync(
            requisition,
            command.ExpectedVersion,
            changedFields,
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return requisition;
    }

    public async Task<Requisition> TransitionAsync(TransitionRequisitionCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var requisition = await LoadAsync(command.RequisitionId, command.ExpectedVersion, actor, cancellationToken);
        RequireActionPermission(command.Action, actor, requisition);

        if (command.Action == RequisitionAction.Close &&
            requisition.Status == RequisitionStatus.ActiveRecruiting &&
            await repository.HasOpenOffersAsync(requisition.Id, cancellationToken))
        {
            throw new CoreHrBusinessRuleException(
                OpenOffersCode,
                "The requisition cannot be closed while an offer is still awaiting the candidate's response.")
            {
                Details = new Dictionary<string, object?> { ["requisitionId"] = requisition.Id }
            };
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = requisition.Apply(command.Action, command.Reason, Today(now), now);

        var saved = await repository.SaveTransitionAsync(
            requisition,
            previousStatus,
            command.Action,
            command.ExpectedVersion,
            Requisition.RecordsReason(command.Action) ? command.Reason?.Trim() : null,
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return requisition;
    }

    private async Task<Requisition> LoadAsync(
        long requisitionId,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var requisition = await repository.GetByIdAsync(requisitionId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, requisitionId);

        if (requisition.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return requisition;
    }

    private async Task RequireReferencesAsync(Requisition requisition, CancellationToken cancellationToken)
    {
        var errors = new ValidationErrors();

        if (!await repository.DepartmentExistsAsync(requisition.DepartmentId, cancellationToken))
        {
            errors.Add("departmentId", "Department does not exist.");
        }

        if (requisition.PositionId is { } positionId &&
            !await repository.PositionExistsAsync(positionId, cancellationToken))
        {
            errors.Add("positionId", "Position does not exist.");
        }

        errors.ThrowIfAny();
    }

    private static void RequireDepartmentInScope(CoreHrActor actor, long departmentId)
    {
        if (!actor.DataScope.CoversDepartment(departmentId))
        {
            throw new CoreHrForbiddenException(
                DepartmentOutOfScopeCode,
                "You may only create or edit requisitions for departments in your data scope.");
        }
    }

    private static void RequireActionPermission(RequisitionAction action, CoreHrActor actor, Requisition requisition)
    {
        switch (action)
        {
            case RequisitionAction.Submit:
                RequireWriteOrCreator(actor, requisition);
                break;
            case RequisitionAction.Approve:
            case RequisitionAction.Reject:
                Require(actor, ApproveForbiddenCode,
                    "Approving or rejecting a requisition requires the recruitment.requisition.approve permission.",
                    RequisitionPermissions.Approve);
                break;
            case RequisitionAction.Publish:
            case RequisitionAction.Close:
                Require(actor, PublishForbiddenCode,
                    "Publishing or closing a requisition requires the recruitment.requisition.publish permission.",
                    RequisitionPermissions.Publish);
                break;
            case RequisitionAction.Cancel:
                Require(actor, CancelForbiddenCode,
                    "Cancelling a requisition requires the recruitment.requisition.write or recruitment.requisition.approve permission.",
                    RequisitionPermissions.Write, RequisitionPermissions.Approve);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action));
        }
    }

    private static void RequireWriteOrCreator(CoreHrActor actor, Requisition requisition)
    {
        if (!actor.HasPermission(RequisitionPermissions.Write) && actor.UserId != requisition.CreatedBy)
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Editing or submitting a requisition requires the recruitment.requisition.write permission or being its creator.");
        }
    }

    private static void Require(CoreHrActor actor, string code, string message, params string[] anyOf)
    {
        if (!anyOf.Any(actor.HasPermission))
        {
            throw new CoreHrForbiddenException(code, message);
        }
    }

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}
