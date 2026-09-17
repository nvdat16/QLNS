using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

public interface IPositionRepository
{
    Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken cancellationToken);

    Task<Position?> GetByIdAsync(long positionId, CancellationToken cancellationToken);

    /// <summary>Case-insensitive code uniqueness check, optionally ignoring one position.</summary>
    Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken);

    /// <summary>Inserts the position and its audit row in one transaction; returns the generated id.</summary>
    Task<long> InsertAsync(Position position, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>Version == expectedVersion</c>; false when no row matched.</summary>
    Task<bool> ReplaceAsync(Position position, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken);
}
