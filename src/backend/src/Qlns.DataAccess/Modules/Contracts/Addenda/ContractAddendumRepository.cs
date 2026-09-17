using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Contracts.Contracts;
using Qlns.DataAccess.Modules.Contracts.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.Contracts.Addenda;

/// <summary>
/// PostgreSQL persistence for <c>contract_addenda</c>. Reads are unscoped because the service decides scope through
/// the parent contract. Every write is one transaction pairing the conditional update with its audit rows; making
/// an addendum effective also writes the superseded addenda and the approved employee event. Audit payloads carry
/// term names, statuses and versions only, never the term values or the object key.
/// </summary>
public sealed class ContractAddendumRepository(QlnsDbContext dbContext) : IContractAddendumRepository
{
    private const string EntityType = "contract_addendum";
    private const string EventEntityType = "employee_event";
    private const string AddendumNumberConstraint = "addendum_number";

    private DbSet<ContractAddendumEntity> Addenda => dbContext.Set<ContractAddendumEntity>();
    private DbSet<EmployeeEventEntity> Events => dbContext.Set<EmployeeEventEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<IReadOnlyList<ContractAddendum>> ListByContractAsync(long contractId, CancellationToken cancellationToken)
    {
        var rows = await Addenda.AsNoTracking()
            .Where(a => a.ContractId == contractId)
            .OrderBy(a => a.EffectiveDate)
            .ThenBy(a => a.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<ContractAddendum?> GetByIdAsync(long addendumId, CancellationToken cancellationToken)
    {
        var entity = await Addenda.AsNoTracking().SingleOrDefaultAsync(a => a.Id == addendumId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> AddendumNumberExistsAsync(string addendumNumber, CancellationToken cancellationToken) =>
        Addenda.AsNoTracking().AnyAsync(a => a.AddendumNumber == addendumNumber, cancellationToken);

    public async Task<ContractAddendum> InsertAsync(ContractAddendum addendum, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(addendum);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new ContractAddendumEntity
        {
            ContractId = addendum.ContractId,
            AddendumNumber = addendum.AddendumNumber,
            Version = addendum.Version,
            Status = addendum.Status.ToContract(),
            EffectiveDate = addendum.EffectiveDate,
            BeforeTerms = addendum.BeforeTerms.ToJsonString(),
            AfterTerms = addendum.AfterTerms.ToJsonString(),
            Reason = addendum.Reason,
            DocumentObjectKey = null,
            CreatedBy = addendum.CreatedBy,
            ApprovedBy = null,
            ApprovedAt = null,
            SignedAt = null,
            CreatedAt = addendum.CreatedAt,
            UpdatedAt = addendum.UpdatedAt
        };

        Addenda.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueViolation.IsOn(exception, AddendumNumberConstraint))
        {
            throw ContractAddendumService.NumberTaken(addendum.AddendumNumber);
        }

        var persisted = ToDomain(entity);
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.addendum.create",
            EntityType,
            persisted.Id,
            before: null,
            after: Snapshot(persisted),
            occurredAt: persisted.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return persisted;
    }

    public async Task<bool> SaveTransitionAsync(
        ContractAddendum addendum,
        ContractAddendumStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (!await UpdateWorkflowColumnsAsync(addendum, expectedVersion, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            TransitionAction(addendum, previousStatus),
            EntityType,
            addendum.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = addendum.Status.ToContract(), version = addendum.Version, signedAt = addendum.SignedAt, reason },
            occurredAt: addendum.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveEffectiveAsync(AddendumActivation activation, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activation);
        var addendum = activation.Addendum;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (!await UpdateWorkflowColumnsAsync(addendum, activation.ExpectedVersion, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        foreach (var superseded in activation.Superseded)
        {
            if (!await UpdateWorkflowColumnsAsync(superseded.Addendum, superseded.ExpectedVersion, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            AuditLogs.Add(CoreHrAudit.Entry(
                actor,
                "contracts.addendum.supersede",
                EntityType,
                superseded.Addendum.Id,
                before: new { status = ContractAddendumStatus.Effective.ToContract(), version = superseded.ExpectedVersion },
                after: new { status = superseded.Addendum.Status.ToContract(), version = superseded.Addendum.Version, supersededByAddendumId = addendum.Id },
                occurredAt: addendum.UpdatedAt));
        }

        long? employeeEventId = null;
        if (activation.EmployeeEvent is { } draft)
        {
            var eventEntity = new EmployeeEventEntity
            {
                EmployeeId = draft.EmployeeId,
                EventType = draft.EventType.ToContract(),
                Status = EmployeeEventStatus.Approved.ToContract(),
                EffectiveDate = draft.EffectiveDate,
                BeforeData = draft.BeforeData.ToJsonString(),
                AfterData = draft.AfterData.ToJsonString(),
                Reason = draft.Reason,
                CompensatesEventId = null,
                CreatedBy = actor.UserId,
                ApprovedBy = actor.UserId,
                ApprovedAt = addendum.UpdatedAt,
                AppliedAt = null,
                CreatedAt = addendum.UpdatedAt,
                UpdatedAt = addendum.UpdatedAt,
                Version = 1
            };

            Events.Add(eventEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
            employeeEventId = eventEntity.Id;

            AuditLogs.Add(CoreHrAudit.Entry(
                actor,
                "corehr.event.create",
                EventEntityType,
                eventEntity.Id,
                before: null,
                after: new
                {
                    status = eventEntity.Status,
                    version = eventEntity.Version,
                    eventType = eventEntity.EventType,
                    effectiveDate = draft.EffectiveDate,
                    fields = draft.AfterData.Select(pair => pair.Key).ToList(),
                    source = EntityType,
                    addendumId = addendum.Id
                },
                occurredAt: addendum.UpdatedAt));
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.addendum.make_effective",
            EntityType,
            addendum.Id,
            before: new { status = ContractAddendumStatus.Approved.ToContract(), version = activation.ExpectedVersion },
            after: new
            {
                status = addendum.Status.ToContract(),
                version = addendum.Version,
                terms = addendum.ChangedTerms,
                supersededAddendumIds = activation.Superseded.Select(s => s.Addendum.Id).ToList(),
                employeeEventId
            },
            occurredAt: addendum.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveSignedDocumentAsync(
        ContractAddendum addendum,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Addenda
            .Where(a => a.Id == addendum.Id && a.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.DocumentObjectKey, addendum.DocumentObjectKey)
                    .SetProperty(a => a.Version, addendum.Version)
                    .SetProperty(a => a.UpdatedAt, addendum.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.addendum.signed_document",
            EntityType,
            addendum.Id,
            before: new { version = expectedVersion },
            after: new { status = addendum.Status.ToContract(), version = addendum.Version, signedDocumentAvailable = true },
            occurredAt: addendum.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private async Task<bool> UpdateWorkflowColumnsAsync(ContractAddendum addendum, long expectedVersion, CancellationToken cancellationToken)
    {
        var rows = await Addenda
            .Where(a => a.Id == addendum.Id && a.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.Status, addendum.Status.ToContract())
                    .SetProperty(a => a.ApprovedBy, addendum.ApprovedBy)
                    .SetProperty(a => a.ApprovedAt, addendum.ApprovedAt)
                    .SetProperty(a => a.SignedAt, addendum.SignedAt)
                    .SetProperty(a => a.Version, addendum.Version)
                    .SetProperty(a => a.UpdatedAt, addendum.UpdatedAt),
                cancellationToken);

        return rows == 1;
    }

    private static string TransitionAction(ContractAddendum addendum, ContractAddendumStatus previousStatus) => addendum.Status switch
    {
        ContractAddendumStatus.PendingApproval => "contracts.addendum.submit",
        ContractAddendumStatus.Approved when previousStatus == ContractAddendumStatus.PendingApproval => "contracts.addendum.approve",
        ContractAddendumStatus.Approved => "contracts.addendum.mark_signed",
        ContractAddendumStatus.Cancelled => "contracts.addendum.cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(addendum), $"{addendum.Status.ToContract()} is not a simple workflow transition target.")
    };

    /// <summary>Audit payload without term values: identifiers, changed term names, status and version only.</summary>
    private static object Snapshot(ContractAddendum addendum) => new
    {
        contractId = addendum.ContractId,
        addendumNumber = addendum.AddendumNumber,
        effectiveDate = addendum.EffectiveDate,
        terms = addendum.ChangedTerms,
        status = addendum.Status.ToContract(),
        version = addendum.Version
    };

    private static ContractAddendum ToDomain(ContractAddendumEntity entity)
    {
        if (!ContractAddendumStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"contract_addenda {entity.Id} has unknown status '{entity.Status}'.");
        }

        return new ContractAddendum(
            entity.Id,
            entity.ContractId,
            entity.AddendumNumber,
            status,
            entity.EffectiveDate,
            ParseObject(entity.BeforeTerms),
            ParseObject(entity.AfterTerms),
            entity.Reason,
            entity.DocumentObjectKey,
            entity.CreatedBy,
            entity.ApprovedBy,
            entity.ApprovedAt,
            entity.SignedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static JsonObject ParseObject(string json) =>
        JsonNode.Parse(json) as JsonObject ?? throw new InvalidOperationException("jsonb column does not hold a JSON object.");
}
