using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.CoreHr.EmployeeEvents;

/// <summary>
/// PostgreSQL persistence for employee_events. Every write is one transaction that pairs the business change
/// (conditional on the expected version) with its audit_logs row. Audit payloads never contain salary values.
/// </summary>
public sealed class EmployeeEventRepository(QlnsDbContext dbContext) : IEmployeeEventRepository
{
    private const string EventEntityType = "employee_event";
    private const string EmployeeEntityType = "employee";

    private DbSet<EmployeeEventEntity> Events => dbContext.Set<EmployeeEventEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<EmployeeMasterData?> GetEmployeeMasterDataAsync(long employeeId, CancellationToken cancellationToken)
    {
        return await Employees.AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => new EmployeeMasterData(
                x.Id,
                x.DepartmentId,
                x.PositionId,
                x.ManagerId,
                x.Status,
                x.WorkEmail,
                x.Version))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<EmployeeEvent>> ListByEmployeeAsync(
        long employeeId,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var query = Events.AsNoTracking().Where(x => x.EmployeeId == employeeId);
        var total = await query.LongCountAsync(cancellationToken);
        if (total == 0)
        {
            return PagedResult<EmployeeEvent>.Empty(page);
        }

        var rows = await query
            .OrderByDescending(x => x.EffectiveDate)
            .ThenByDescending(x => x.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeEvent>(rows.Select(ToDomain).ToList(), page.Page, page.PageSize, total);
    }

    public async Task<EmployeeEvent?> GetByIdAsync(long eventId, CancellationToken cancellationToken)
    {
        var entity = await Events.AsNoTracking().SingleOrDefaultAsync(x => x.Id == eventId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<ConflictingEvent>> FindConflictingAsync(
        long employeeId,
        DateOnly effectiveDate,
        IReadOnlyCollection<string> fields,
        long? excludeEventId,
        CancellationToken cancellationToken)
    {
        if (fields.Count == 0)
        {
            return [];
        }

        var cancelled = EmployeeEventStatus.Cancelled.ToContract();
        var candidates = await Events.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId &&
                x.EffectiveDate == effectiveDate &&
                x.Status != cancelled &&
                (excludeEventId == null || x.Id != excludeEventId))
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.AfterData })
            .ToListAsync(cancellationToken);

        var requested = fields.ToHashSet(StringComparer.Ordinal);
        var conflicts = new List<ConflictingEvent>();
        foreach (var candidate in candidates)
        {
            var shared = ParseObject(candidate.AfterData)
                .Select(pair => pair.Key)
                .Where(requested.Contains)
                .ToList();

            if (shared.Count > 0)
            {
                conflicts.Add(new ConflictingEvent(candidate.Id, shared));
            }
        }

        return conflicts;
    }

    public async Task<EmployeeEvent> InsertAsync(
        EmployeeEvent employeeEvent,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new EmployeeEventEntity
        {
            EmployeeId = employeeEvent.EmployeeId,
            EventType = employeeEvent.EventType.ToContract(),
            Status = employeeEvent.Status.ToContract(),
            EffectiveDate = employeeEvent.EffectiveDate,
            BeforeData = employeeEvent.BeforeData.ToJsonString(),
            AfterData = employeeEvent.AfterData.ToJsonString(),
            Reason = employeeEvent.Reason,
            CompensatesEventId = employeeEvent.CompensatesEventId,
            CreatedBy = employeeEvent.CreatedBy,
            ApprovedBy = employeeEvent.ApprovedBy,
            ApprovedAt = employeeEvent.ApprovedAt,
            AppliedAt = employeeEvent.AppliedAt,
            CreatedAt = employeeEvent.CreatedAt,
            UpdatedAt = employeeEvent.UpdatedAt,
            Version = employeeEvent.Version
        };

        Events.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var persisted = ToDomain(entity);
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.event.create",
            EventEntityType,
            persisted.Id,
            before: null,
            after: AuditSnapshot(persisted),
            occurredAt: persisted.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return persisted;
    }

    public async Task<bool> SaveTransitionAsync(
        EmployeeEvent employeeEvent,
        EmployeeEventStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Events
            .Where(x => x.Id == employeeEvent.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, employeeEvent.Status.ToContract())
                    .SetProperty(x => x.ApprovedBy, employeeEvent.ApprovedBy)
                    .SetProperty(x => x.ApprovedAt, employeeEvent.ApprovedAt)
                    .SetProperty(x => x.Version, employeeEvent.Version)
                    .SetProperty(x => x.UpdatedAt, employeeEvent.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            TransitionAction(employeeEvent.Status),
            EventEntityType,
            employeeEvent.Id,
            before: AuditSnapshot(employeeEvent, previousStatus, expectedVersion),
            after: AuditSnapshot(employeeEvent, reason),
            occurredAt: employeeEvent.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<EmployeeEvent>> ListDueApprovedAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var approved = EmployeeEventStatus.Approved.ToContract();
        var rows = await Events.AsNoTracking()
            .Where(x => x.Status == approved && x.EffectiveDate <= today)
            .OrderBy(x => x.EffectiveDate)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<bool> SaveAppliedAsync(
        EmployeeEvent employeeEvent,
        long expectedEventVersion,
        EmployeeMasterData newData,
        long expectedEmployeeVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var eventRows = await Events
            .Where(x => x.Id == employeeEvent.Id && x.Version == expectedEventVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, employeeEvent.Status.ToContract())
                    .SetProperty(x => x.AppliedAt, employeeEvent.AppliedAt)
                    .SetProperty(x => x.Version, employeeEvent.Version)
                    .SetProperty(x => x.UpdatedAt, employeeEvent.UpdatedAt),
                cancellationToken);

        if (eventRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var previous = await Employees.AsNoTracking()
            .Where(x => x.Id == newData.EmployeeId)
            .Select(x => new { x.DepartmentId, x.PositionId, x.ManagerId, x.Status, x.Version })
            .SingleOrDefaultAsync(cancellationToken);

        var employeeRows = await Employees
            .Where(x => x.Id == newData.EmployeeId && x.Version == expectedEmployeeVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.DepartmentId, newData.DepartmentId)
                    .SetProperty(x => x.PositionId, newData.PositionId)
                    .SetProperty(x => x.ManagerId, newData.ManagerId)
                    .SetProperty(x => x.Status, newData.Status)
                    .SetProperty(x => x.UpdatedAt, employeeEvent.UpdatedAt)
                    .SetProperty(x => x.Version, expectedEmployeeVersion + 1),
                cancellationToken);

        if (employeeRows != 1 || previous is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.event.apply",
            EventEntityType,
            employeeEvent.Id,
            before: AuditSnapshot(employeeEvent, EmployeeEventStatus.Approved, expectedEventVersion),
            after: AuditSnapshot(employeeEvent),
            occurredAt: employeeEvent.UpdatedAt));

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.employee.master_data.apply_event",
            EmployeeEntityType,
            newData.EmployeeId,
            before: new
            {
                departmentId = previous.DepartmentId,
                positionId = previous.PositionId,
                managerId = previous.ManagerId,
                status = previous.Status,
                version = previous.Version
            },
            after: new
            {
                departmentId = newData.DepartmentId,
                positionId = newData.PositionId,
                managerId = newData.ManagerId,
                status = newData.Status,
                version = expectedEmployeeVersion + 1,
                employeeEventId = employeeEvent.Id
            },
            occurredAt: employeeEvent.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static string TransitionAction(EmployeeEventStatus newStatus) => newStatus switch
    {
        EmployeeEventStatus.PendingApproval => "corehr.event.submit",
        EmployeeEventStatus.Approved => "corehr.event.approve",
        EmployeeEventStatus.Cancelled => "corehr.event.cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(newStatus), "Not a workflow transition status.")
    };

    /// <summary>Audit payload without field values: salary and other restricted data never reach audit_logs.</summary>
    private static object AuditSnapshot(EmployeeEvent employeeEvent, string? reason = null) =>
        AuditSnapshot(employeeEvent, employeeEvent.Status, employeeEvent.Version, reason);

    private static object AuditSnapshot(EmployeeEvent employeeEvent, EmployeeEventStatus status, long version, string? reason = null) => new
    {
        status = status.ToContract(),
        version,
        fields = employeeEvent.ChangedFields,
        reason
    };

    private static EmployeeEvent ToDomain(EmployeeEventEntity entity)
    {
        if (!EmployeeEventTypeNames.TryParseContract(entity.EventType, out var type))
        {
            throw new InvalidOperationException($"employee_events {entity.Id} has unknown event_type '{entity.EventType}'.");
        }

        if (!EmployeeEventStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"employee_events {entity.Id} has unknown status '{entity.Status}'.");
        }

        return new EmployeeEvent(
            entity.Id,
            entity.EmployeeId,
            type,
            status,
            entity.EffectiveDate,
            ParseObject(entity.BeforeData),
            ParseObject(entity.AfterData),
            entity.Reason,
            entity.CompensatesEventId,
            entity.CreatedBy,
            entity.ApprovedBy,
            entity.ApprovedAt,
            entity.AppliedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static JsonObject ParseObject(string json) =>
        JsonNode.Parse(json) as JsonObject ?? throw new InvalidOperationException("jsonb column does not hold a JSON object.");
}
