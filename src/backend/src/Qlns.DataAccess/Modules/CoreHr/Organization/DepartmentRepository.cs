using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Organization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.CoreHr.Organization;

public sealed class DepartmentRepository(QlnsDbContext dbContext) : IDepartmentRepository
{
    private const string EntityType = "department";
    private const string TerminatedStatus = "terminated";

    /// <summary>job_postings.status values that count as an open requisition.</summary>
    private static readonly string[] OpenRequisitionStatuses =
        ["draft", "pending_approval", "approved", "active_recruiting"];

    private DbSet<DepartmentEntity> Departments => dbContext.Set<DepartmentEntity>();

    public async Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await Departments.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Department?> GetByIdAsync(long departmentId, CancellationToken cancellationToken)
    {
        var entity = await Departments.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == departmentId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyDictionary<long, int>> GetHeadcountsAsync(CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<EmployeeEntity>().AsNoTracking()
            .Where(x => x.Status != TerminatedStatus)
            .GroupBy(x => x.DepartmentId)
            .Select(g => new DepartmentHeadcount(g.Key, g.Count()))
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(x => x.DepartmentId, x => x.Headcount);
    }

    public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        var normalized = code.ToLowerInvariant();
        return Departments.AsNoTracking()
            .AnyAsync(x => x.Code.ToLower() == normalized && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public Task<bool> ExistsAsync(long departmentId, CancellationToken cancellationToken) =>
        Departments.AsNoTracking().AnyAsync(x => x.Id == departmentId, cancellationToken);

    public async Task<DepartmentDependencies> GetDependenciesAsync(long departmentId, CancellationToken cancellationToken)
    {
        var childDepartments = await Departments.AsNoTracking()
            .CountAsync(x => x.ParentDepartmentId == departmentId, cancellationToken);

        var employees = await dbContext.Set<EmployeeEntity>().AsNoTracking()
            .CountAsync(x => x.DepartmentId == departmentId, cancellationToken);

        var openRequisitions = await dbContext.Set<JobPostingEntity>().AsNoTracking()
            .CountAsync(x => x.DepartmentId == departmentId && OpenRequisitionStatuses.Contains(x.Status), cancellationToken);

        return new DepartmentDependencies(childDepartments, employees, openRequisitions);
    }

    public async Task<long> InsertAsync(Department department, CoreHrActor actor, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new DepartmentEntity
        {
            Code = department.Code,
            Name = department.Name,
            ParentDepartmentId = department.ParentDepartmentId,
            CostCenter = department.CostCenter,
            Description = department.Description,
            CreatedAt = department.CreatedAt,
            UpdatedAt = department.UpdatedAt,
            Version = department.Version
        };

        Departments.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            "corehr.department.create",
            EntityType,
            entity.Id,
            before: null,
            after: Snapshot(entity),
            occurredAt: department.CreatedAt));
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> ReplaceAsync(
        Department department,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var before = await Departments.AsNoTracking()
            .Where(x => x.Id == department.Id && x.Version == expectedVersion)
            .Select(x => Snapshot(x))
            .SingleOrDefaultAsync(cancellationToken);

        var rows = await Departments
            .Where(x => x.Id == department.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Code, department.Code)
                    .SetProperty(x => x.Name, department.Name)
                    .SetProperty(x => x.ParentDepartmentId, department.ParentDepartmentId)
                    .SetProperty(x => x.CostCenter, department.CostCenter)
                    .SetProperty(x => x.Description, department.Description)
                    .SetProperty(x => x.UpdatedAt, department.UpdatedAt)
                    .SetProperty(x => x.Version, department.Version),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            "corehr.department.replace",
            EntityType,
            department.Id,
            before,
            after: Snapshot(department),
            occurredAt: department.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        long departmentId,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var before = await Departments.AsNoTracking()
            .Where(x => x.Id == departmentId && x.Version == expectedVersion)
            .Select(x => Snapshot(x))
            .SingleOrDefaultAsync(cancellationToken);

        var rows = await Departments
            .Where(x => x.Id == departmentId && x.Version == expectedVersion)
            .ExecuteDeleteAsync(cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            "corehr.department.delete",
            EntityType,
            departmentId,
            before,
            after: null,
            occurredAt: DateTimeOffset.UtcNow));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static Department ToDomain(DepartmentEntity entity) => new(
        entity.Id,
        entity.Code,
        entity.Name,
        entity.ParentDepartmentId,
        entity.CostCenter,
        entity.Description,
        entity.Version,
        entity.CreatedAt,
        entity.UpdatedAt);

    private static DepartmentSnapshot Snapshot(DepartmentEntity entity) => new(
        entity.Code, entity.Name, entity.ParentDepartmentId, entity.CostCenter, entity.Description, entity.Version);

    private static DepartmentSnapshot Snapshot(Department department) => new(
        department.Code, department.Name, department.ParentDepartmentId, department.CostCenter, department.Description, department.Version);

    /// <summary>Audit payload (before/after). Contains no restricted personal data.</summary>
    private sealed record DepartmentSnapshot(
        string Code,
        string Name,
        long? ParentDepartmentId,
        string? CostCenter,
        string? Description,
        long Version);
}
