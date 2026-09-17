using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.CoreHr.Offboarding;

/// <summary>
/// PostgreSQL persistence for offboarding_tasks. Reads return the owning case's employee and status so the
/// service decides visibility (department scope, self, or assignee); writes pair the conditional status update with its audit row.
/// </summary>
public sealed class OffboardingTaskRepository(QlnsDbContext dbContext) : IOffboardingTaskRepository
{
    private const string EntityType = "offboarding_task";

    private DbSet<OffboardingTaskEntity> Tasks => dbContext.Set<OffboardingTaskEntity>();
    private DbSet<OffboardingCaseEntity> Cases => dbContext.Set<OffboardingCaseEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<IReadOnlyList<OffboardingTask>> ListByCaseAsync(
        long caseId,
        OffboardingTaskFilter filter,
        CancellationToken cancellationToken)
    {
        var tasks = Tasks.AsNoTracking().Where(t => t.OffboardingCaseId == caseId);

        if (filter.Category is { } category)
        {
            var categoryValue = category.ToContract();
            tasks = tasks.Where(t => t.Category == categoryValue);
        }

        if (filter.BlockingOnly == true)
        {
            tasks = tasks.Where(t => t.BlocksLastWorkingDay);
        }

        var entities = await tasks.OrderBy(t => t.Id).ToListAsync(cancellationToken);
        return entities.Select(OffboardingMapping.ToDomain).ToList();
    }

    public async Task<OffboardingTaskEntry?> GetByIdAsync(long taskId, CancellationToken cancellationToken)
    {
        var row = await (
            from task in Tasks.AsNoTracking()
            join offboardingCase in Cases.AsNoTracking() on task.OffboardingCaseId equals offboardingCase.Id
            join employee in Employees.AsNoTracking() on offboardingCase.EmployeeId equals employee.Id
            where task.Id == taskId
            select new { Task = task, offboardingCase.EmployeeId, employee.DepartmentId, CaseStatus = offboardingCase.Status })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new OffboardingTaskEntry(
                OffboardingMapping.ToDomain(row.Task),
                row.EmployeeId,
                row.DepartmentId,
                OffboardingMapping.ParseCaseStatus(row.Task.OffboardingCaseId, row.CaseStatus));
    }

    public async Task<bool> SaveTransitionAsync(
        OffboardingTask task,
        OffboardingTaskStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Tasks
            .Where(t => t.Id == task.Id && t.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.Status, task.Status.ToContract())
                    .SetProperty(t => t.CompletedAt, task.CompletedAt)
                    .SetProperty(t => t.Version, task.Version)
                    .SetProperty(t => t.UpdatedAt, task.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            TransitionAction(previousStatus, task.Status),
            EntityType,
            task.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = task.Status.ToContract(), version = task.Version, templateKey = task.TemplateKey, reason },
            occurredAt: task.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static string TransitionAction(OffboardingTaskStatus previous, OffboardingTaskStatus current) =>
        (previous, current) switch
        {
            (OffboardingTaskStatus.Pending, OffboardingTaskStatus.InProgress) => "corehr.offboarding.task.start",
            (OffboardingTaskStatus.InProgress, OffboardingTaskStatus.Completed) => "corehr.offboarding.task.complete",
            (OffboardingTaskStatus.Completed, OffboardingTaskStatus.Pending) => "corehr.offboarding.task.reopen",
            _ => throw new InvalidOperationException(
                $"Unsupported offboarding task transition {previous.ToContract()} → {current.ToContract()}.")
        };
}
