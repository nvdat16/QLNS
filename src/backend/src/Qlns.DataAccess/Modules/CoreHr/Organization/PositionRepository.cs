using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Organization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.CoreHr.Organization;

public sealed class PositionRepository(QlnsDbContext dbContext) : IPositionRepository
{
    private const string EntityType = "position";

    private DbSet<PositionEntity> Positions => dbContext.Set<PositionEntity>();

    public async Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await Positions.AsNoTracking()
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<Position?> GetByIdAsync(long positionId, CancellationToken cancellationToken)
    {
        var entity = await Positions.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == positionId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        var normalized = code.ToLowerInvariant();
        return Positions.AsNoTracking()
            .AnyAsync(x => x.Code.ToLower() == normalized && (excludeId == null || x.Id != excludeId), cancellationToken);
    }

    public async Task<long> InsertAsync(Position position, CoreHrActor actor, CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new PositionEntity
        {
            Code = position.Code,
            Name = position.Name,
            Level = position.Level,
            Description = position.Description,
            CreatedAt = position.CreatedAt,
            UpdatedAt = position.UpdatedAt,
            Version = position.Version
        };

        Positions.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            "corehr.position.create",
            EntityType,
            entity.Id,
            before: null,
            after: Snapshot(entity),
            occurredAt: position.CreatedAt));
        await dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return entity.Id;
    }

    public async Task<bool> ReplaceAsync(
        Position position,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var before = await Positions.AsNoTracking()
            .Where(x => x.Id == position.Id && x.Version == expectedVersion)
            .Select(x => Snapshot(x))
            .SingleOrDefaultAsync(cancellationToken);

        var rows = await Positions
            .Where(x => x.Id == position.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Code, position.Code)
                    .SetProperty(x => x.Name, position.Name)
                    .SetProperty(x => x.Level, position.Level)
                    .SetProperty(x => x.Description, position.Description)
                    .SetProperty(x => x.UpdatedAt, position.UpdatedAt)
                    .SetProperty(x => x.Version, position.Version),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            "corehr.position.replace",
            EntityType,
            position.Id,
            before,
            after: Snapshot(position),
            occurredAt: position.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static Position ToDomain(PositionEntity entity) => new(
        entity.Id,
        entity.Code,
        entity.Name,
        entity.Level,
        entity.Description,
        entity.Version,
        entity.CreatedAt,
        entity.UpdatedAt);

    private static PositionSnapshot Snapshot(PositionEntity entity) => new(
        entity.Code, entity.Name, entity.Level, entity.Description, entity.Version);

    private static PositionSnapshot Snapshot(Position position) => new(
        position.Code, position.Name, position.Level, position.Description, position.Version);

    /// <summary>Audit payload (before/after).</summary>
    private sealed record PositionSnapshot(string Code, string Name, string? Level, string? Description, long Version);
}
