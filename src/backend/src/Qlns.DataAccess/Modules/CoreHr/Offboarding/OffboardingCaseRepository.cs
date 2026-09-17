using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Contracts.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.CoreHr.Offboarding;

/// <summary>
/// PostgreSQL persistence for offboarding_cases. Searches join employees so the actor's data scope is applied in SQL;
/// every write is one transaction pairing the business change (conditional on the expected version) with its audit
/// row, the generated checklist (approve) or the approved termination event and outbox message (complete).
/// </summary>
public sealed class OffboardingCaseRepository(QlnsDbContext dbContext) : IOffboardingCaseRepository
{
    private const string CaseEntityType = "offboarding_case";
    private const string EventEntityType = "employee_event";
    private const string OpenCaseIndex = "ux_offboarding_open_case";

    /// <summary>contracts.status values whose notice_period_days governs the current employment.</summary>
    private static readonly string[] NoticeContractStatuses = ["executed", "active"];

    private DbSet<OffboardingCaseEntity> Cases => dbContext.Set<OffboardingCaseEntity>();
    private DbSet<OffboardingTaskEntity> Tasks => dbContext.Set<OffboardingTaskEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<ContractEntity> Contracts => dbContext.Set<ContractEntity>();
    private DbSet<EmployeeEventEntity> Events => dbContext.Set<EmployeeEventEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();
    private DbSet<OutboxMessageEntity> Outbox => dbContext.Set<OutboxMessageEntity>();

    public Task<OffboardingEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
        Employees.AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new OffboardingEmployee(
                e.Id,
                e.DepartmentId,
                e.Status,
                Employees.Where(m => m.Id == e.ManagerId).Select(m => m.UserId).FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<int?> GetNoticePeriodDaysAsync(long employeeId, CancellationToken cancellationToken) =>
        Contracts.AsNoTracking()
            .Where(c => c.EmployeeId == employeeId && c.IsPrimary && NoticeContractStatuses.Contains(c.Status))
            .OrderByDescending(c => c.StartDate)
            .Select(c => c.NoticePeriodDays)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasOpenCaseAsync(long employeeId, CancellationToken cancellationToken) =>
        Cases.AsNoTracking()
            .AnyAsync(c => c.EmployeeId == employeeId && OffboardingMapping.OpenCaseStatuses.Contains(c.Status), cancellationToken);

    public async Task<PagedResult<OffboardingCaseEntry>> SearchAsync(
        OffboardingCaseSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var rows = CasesWithEmployees();

        if (!actor.DataScope.OrganizationWide)
        {
            var departmentIds = actor.DataScope.DepartmentIds.ToArray();
            var selfId = actor.EmployeeId ?? 0;
            rows = rows.Where(x => departmentIds.Contains(x.Employee.DepartmentId) || x.Case.EmployeeId == selfId);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            rows = rows.Where(x => x.Case.Status == statusValue);
        }

        if (query.DepartmentId is { } departmentId)
        {
            rows = rows.Where(x => x.Employee.DepartmentId == departmentId);
        }

        var totalItems = await rows.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<OffboardingCaseEntry>.Empty(query.Page);
        }

        var page = await Project(rows
                .OrderBy(x => x.Case.LastWorkingDate)
                .ThenBy(x => x.Case.Id)
                .Skip(query.Page.Skip)
                .Take(query.Page.PageSize))
            .ToListAsync(cancellationToken);

        return new PagedResult<OffboardingCaseEntry>(
            page.Select(ToEntry).ToList(),
            query.Page.Page,
            query.Page.PageSize,
            totalItems);
    }

    public async Task<OffboardingCaseEntry?> GetByIdAsync(long caseId, CancellationToken cancellationToken)
    {
        var row = await Project(CasesWithEmployees().Where(x => x.Case.Id == caseId))
            .SingleOrDefaultAsync(cancellationToken);
        return row is null ? null : ToEntry(row);
    }

    public async Task<IReadOnlyList<OffboardingTask>> ListBlockingTasksAsync(long caseId, CancellationToken cancellationToken)
    {
        var entities = await Tasks.AsNoTracking()
            .Where(t => t.OffboardingCaseId == caseId && t.BlocksLastWorkingDay)
            .OrderBy(t => t.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(OffboardingMapping.ToDomain).ToList();
    }

    public async Task<IReadOnlySet<string>> ListTemplateKeysAsync(long caseId, CancellationToken cancellationToken)
    {
        var keys = await Tasks.AsNoTracking()
            .Where(t => t.OffboardingCaseId == caseId)
            .Select(t => t.TemplateKey)
            .ToListAsync(cancellationToken);

        return keys.ToHashSet(StringComparer.Ordinal);
    }

    public async Task<OffboardingCase> InsertAsync(
        OffboardingCase offboardingCase,
        int? noticePeriodShortfallDays,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = OffboardingMapping.ToEntity(offboardingCase);
        Cases.Add(entity);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, OpenCaseIndex))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new CoreHrBusinessRuleException(
                OffboardingCase.CaseOpenCode,
                $"Employee {offboardingCase.EmployeeId} already has an open offboarding case.")
            {
                Details = new Dictionary<string, object?> { ["employeeId"] = offboardingCase.EmployeeId }
            };
        }

        // The notice-period warning is not a business error, so the audit row is where it is preserved (EMP-07.1 scenario 3).
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.offboarding.case.create",
            CaseEntityType,
            entity.Id,
            before: null,
            after: new
            {
                status = entity.Status,
                version = entity.Version,
                employeeId = entity.EmployeeId,
                separationType = entity.SeparationType,
                lastWorkingDate = entity.LastWorkingDate,
                noticeReceivedOn = entity.NoticeReceivedOn,
                handoverToEmployeeId = entity.HandoverToEmployeeId,
                noticePeriodShortfallDays
            },
            occurredAt: entity.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OffboardingMapping.ToDomain(entity);
    }

    public async Task<bool> SaveApprovalAsync(
        OffboardingCase offboardingCase,
        OffboardingCaseStatus previousStatus,
        IReadOnlyList<OffboardingTask> generatedTasks,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Cases
            .Where(c => c.Id == offboardingCase.Id && c.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, offboardingCase.Status.ToContract())
                    .SetProperty(c => c.ApprovedBy, offboardingCase.ApprovedBy)
                    .SetProperty(c => c.ApprovedAt, offboardingCase.ApprovedAt)
                    .SetProperty(c => c.Version, offboardingCase.Version)
                    .SetProperty(c => c.UpdatedAt, offboardingCase.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        Tasks.AddRange(generatedTasks.Select(OffboardingMapping.ToEntity));

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.offboarding.case.approve",
            CaseEntityType,
            offboardingCase.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new
            {
                status = offboardingCase.Status.ToContract(),
                version = offboardingCase.Version,
                generatedTasks = generatedTasks.Select(t => t.TemplateKey).ToList()
            },
            occurredAt: offboardingCase.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveTransitionAsync(
        OffboardingCase offboardingCase,
        OffboardingCaseStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Cases
            .Where(c => c.Id == offboardingCase.Id && c.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, offboardingCase.Status.ToContract())
                    .SetProperty(c => c.Version, offboardingCase.Version)
                    .SetProperty(c => c.UpdatedAt, offboardingCase.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            TransitionAction(offboardingCase.Status),
            CaseEntityType,
            offboardingCase.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = offboardingCase.Status.ToContract(), version = offboardingCase.Version, reason },
            occurredAt: offboardingCase.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveCompletionAsync(
        OffboardingCase offboardingCase,
        ApprovedEmployeeEvent? terminationEvent,
        long expectedVersion,
        string? overrideReason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = offboardingCase.UpdatedAt;
        var employeeEventId = offboardingCase.EmployeeEventId;
        EmployeeEventEntity? eventEntity = null;
        if (terminationEvent is not null)
        {
            eventEntity = OffboardingMapping.ToApprovedEventEntity(terminationEvent, actor.UserId, now);
            Events.Add(eventEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
            employeeEventId = eventEntity.Id;
        }

        var rows = await Cases
            .Where(c => c.Id == offboardingCase.Id && c.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, offboardingCase.Status.ToContract())
                    .SetProperty(c => c.CompletedAt, offboardingCase.CompletedAt)
                    .SetProperty(c => c.EmployeeEventId, employeeEventId)
                    .SetProperty(c => c.Version, offboardingCase.Version)
                    .SetProperty(c => c.UpdatedAt, now),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        if (eventEntity is not null && terminationEvent is not null)
        {
            offboardingCase.LinkEmployeeEvent(eventEntity.Id);
            AuditLogs.Add(CoreHrAudit.Entry(
                actor,
                "corehr.event.create",
                EventEntityType,
                eventEntity.Id,
                before: null,
                after: OffboardingMapping.ApprovedEventAuditSnapshot(terminationEvent, CaseEntityType, offboardingCase.Id),
                occurredAt: now));
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.offboarding.case.complete",
            CaseEntityType,
            offboardingCase.Id,
            before: new { status = OffboardingCaseStatus.InProgress.ToContract(), version = expectedVersion },
            after: new
            {
                status = offboardingCase.Status.ToContract(),
                version = offboardingCase.Version,
                employeeEventId,
                lastWorkingDate = offboardingCase.LastWorkingDate,
                @override = overrideReason is not null,
                overrideReason
            },
            occurredAt: now));

        Outbox.Add(CoreHrOutbox.Message(
            "corehr.offboarding.case_completed",
            CaseEntityType,
            offboardingCase.Id,
            new
            {
                caseId = offboardingCase.Id,
                employeeId = offboardingCase.EmployeeId,
                lastWorkingDate = offboardingCase.LastWorkingDate,
                employeeEventId
            },
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private IQueryable<CaseWithEmployee> CasesWithEmployees() =>
        from offboardingCase in Cases.AsNoTracking()
        join employee in Employees.AsNoTracking() on offboardingCase.EmployeeId equals employee.Id
        select new CaseWithEmployee { Case = offboardingCase, Employee = employee };

    /// <summary>Adds the derived columns: manager's user id, outstanding blocking tasks and contractual notice period.</summary>
    private IQueryable<CaseRow> Project(IQueryable<CaseWithEmployee> rows)
    {
        var completed = OffboardingTaskStatus.Completed.ToContract();
        return rows.Select(x => new CaseRow(
            x.Case,
            x.Employee.Id,
            x.Employee.DepartmentId,
            x.Employee.Status,
            Employees.Where(m => m.Id == x.Employee.ManagerId).Select(m => m.UserId).FirstOrDefault(),
            Tasks.Count(t => t.OffboardingCaseId == x.Case.Id && t.BlocksLastWorkingDay && t.Status != completed),
            Contracts
                .Where(c => c.EmployeeId == x.Case.EmployeeId && c.IsPrimary && NoticeContractStatuses.Contains(c.Status))
                .OrderByDescending(c => c.StartDate)
                .Select(c => c.NoticePeriodDays)
                .FirstOrDefault()));
    }

    private static OffboardingCaseEntry ToEntry(CaseRow row) => new(
        OffboardingMapping.ToDomain(row.Case),
        new OffboardingEmployee(row.EmployeeId, row.DepartmentId, row.EmployeeStatus, row.ManagerUserId),
        row.BlockingTasksOutstanding,
        row.NoticePeriodDays);

    private static string TransitionAction(OffboardingCaseStatus newStatus) => newStatus switch
    {
        OffboardingCaseStatus.InProgress => "corehr.offboarding.case.start",
        OffboardingCaseStatus.Cancelled => "corehr.offboarding.case.cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(newStatus), "Not a plain workflow transition status.")
    };

    private static bool IsUniqueViolation(DbUpdateException exception, string constraintName) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation
        } postgres && string.Equals(postgres.ConstraintName, constraintName, StringComparison.Ordinal);

    private sealed class CaseWithEmployee
    {
        public OffboardingCaseEntity Case { get; init; } = null!;
        public EmployeeEntity Employee { get; init; } = null!;
    }

    private sealed record CaseRow(
        OffboardingCaseEntity Case,
        long EmployeeId,
        long DepartmentId,
        string EmployeeStatus,
        long? ManagerUserId,
        int BlockingTasksOutstanding,
        int? NoticePeriodDays);
}
