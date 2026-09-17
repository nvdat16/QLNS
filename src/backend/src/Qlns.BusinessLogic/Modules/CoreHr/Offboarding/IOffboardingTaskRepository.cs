using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>Persistence contract for offboarding checklist tasks. Visibility is decided by the service from the entry.</summary>
public interface IOffboardingTaskRepository
{
    /// <summary>Tasks of one case ordered by category then template key, optionally filtered.</summary>
    Task<IReadOnlyList<OffboardingTask>> ListByCaseAsync(
        long caseId,
        OffboardingTaskFilter filter,
        CancellationToken cancellationToken);

    Task<OffboardingTaskEntry?> GetByIdAsync(long taskId, CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>id AND version = expectedVersion</c> plus audit row; false when no row matched.</summary>
    Task<bool> SaveTransitionAsync(
        OffboardingTask task,
        OffboardingTaskStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
