using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Onboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.CoreHr.Onboarding;

/// <summary>
/// PostgreSQL persistence for onboarding_tasks. Every read joins employees so the actor's data scope
/// (organization, department, assignee or self) is applied in SQL; tasks outside scope are simply absent.
/// </summary>
public sealed class OnboardingTaskRepository(QlnsDbContext dbContext) : IOnboardingTaskRepository
{
    private const string EntityType = "onboarding_task";

    public async Task<PagedResult<OnboardingTask>> SearchAsync(
        OnboardingTaskSearchQuery query,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var tasks = Visible(actor);

        if (query.EmployeeId is { } employeeId)
        {
            tasks = tasks.Where(t => t.EmployeeId == employeeId);
        }

        if (query.AssignedToUserId is { } assignedToUserId)
        {
            tasks = tasks.Where(t => t.AssignedToUserId == assignedToUserId);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            tasks = tasks.Where(t => t.Status == statusValue);
        }

        if (query.Overdue is { } overdue)
        {
            var completed = OnboardingTaskStatus.Completed.ToContract();
            tasks = overdue
                ? tasks.Where(t => t.DueAt != null && t.DueAt < now && t.Status != completed)
                : tasks.Where(t => t.DueAt == null || t.DueAt >= now || t.Status == completed);
        }

        var totalItems = await tasks.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<OnboardingTask>.Empty(query.Page);
        }

        var entities = await tasks
            .OrderBy(t => t.DueAt == null)
            .ThenBy(t => t.DueAt)
            .ThenBy(t => t.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        var items = entities
            .Select(ToDomain)
            .Where(task => task is not null)
            .Select(task => task!)
            .ToList();

        return new PagedResult<OnboardingTask>(items, query.Page.Page, query.Page.PageSize, totalItems);
    }

    public async Task<OnboardingTask?> GetByIdAsync(
        long taskId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var entity = await Visible(actor)
            .SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken) =>
        dbContext.Set<UserEntity>()
            .AsNoTracking()
            .AnyAsync(u => u.Id == userId && u.Status == "active", cancellationToken);

    public async Task<bool> SaveTransitionAsync(
        OnboardingTask task,
        OnboardingTaskStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Set<OnboardingTaskEntity>()
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

        var action = TransitionAction(previousStatus, task.Status);
        object before = new { status = previousStatus.ToContract(), version = expectedVersion };
        object after = action == "corehr.onboarding.task.reopen"
            ? new { status = task.Status.ToContract(), version = task.Version, reason }
            : new { status = task.Status.ToContract(), version = task.Version };

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            action,
            EntityType,
            task.Id,
            before,
            after,
            task.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveAssignmentAsync(
        OnboardingTask task,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Set<OnboardingTaskEntity>()
            .Where(t => t.Id == task.Id && t.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.TaskName, task.TaskName)
                    .SetProperty(t => t.Description, task.Description)
                    .SetProperty(t => t.AssignedToUserId, task.AssignedToUserId)
                    .SetProperty(t => t.DueAt, task.DueAt)
                    .SetProperty(t => t.Version, task.Version)
                    .SetProperty(t => t.UpdatedAt, task.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            "corehr.onboarding.task.update",
            EntityType,
            task.Id,
            new { version = expectedVersion },
            new { version = task.Version, changedFields },
            task.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Tasks the actor may see: organization-wide scope, the task's employee belongs to one of the
    /// actor's departments, the task is assigned to the actor, or the task belongs to the actor's own record.
    /// </summary>
    private IQueryable<OnboardingTaskEntity> Visible(CoreHrActor actor)
    {
        var tasks = dbContext.Set<OnboardingTaskEntity>().AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return tasks;
        }

        var departmentIds = actor.DataScope.DepartmentIds;
        var userId = actor.UserId;
        var employeeId = actor.EmployeeId;

        return
            from task in tasks
            join employee in dbContext.Set<EmployeeEntity>().AsNoTracking()
                on task.EmployeeId equals employee.Id
            where departmentIds.Contains(employee.DepartmentId) ||
                task.AssignedToUserId == userId ||
                (employeeId != null && task.EmployeeId == employeeId)
            select task;
    }

    private static string TransitionAction(OnboardingTaskStatus previous, OnboardingTaskStatus current) =>
        (previous, current) switch
        {
            (OnboardingTaskStatus.Pending, OnboardingTaskStatus.InProgress) => "corehr.onboarding.task.start",
            (OnboardingTaskStatus.InProgress, OnboardingTaskStatus.Completed) => "corehr.onboarding.task.complete",
            (OnboardingTaskStatus.Completed, OnboardingTaskStatus.Pending) => "corehr.onboarding.task.reopen",
            _ => throw new InvalidOperationException(
                $"Unsupported onboarding transition {previous.ToContract()} → {current.ToContract()}.")
        };

    private static OnboardingTask? ToDomain(OnboardingTaskEntity entity)
    {
        if (!OnboardingTaskStatusNames.TryParseContract(entity.Status, out var status))
        {
            return null;
        }

        return new OnboardingTask(
            entity.Id,
            entity.EmployeeId,
            entity.TemplateKey,
            entity.TaskName,
            entity.Description,
            entity.AssignedToUserId,
            entity.DueAt,
            status,
            entity.CompletedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
