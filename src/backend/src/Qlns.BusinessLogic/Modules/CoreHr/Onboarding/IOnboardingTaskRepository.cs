using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>
/// Persistence contract for onboarding tasks. Implementations apply the actor's data scope on every
/// read (organization-wide, department of the task's employee, assignee, or the actor's own employee record)
/// and write business change plus audit row in one transaction.
/// </summary>
public interface IOnboardingTaskRepository
{
    Task<PagedResult<OnboardingTask>> SearchAsync(
        OnboardingTaskSearchQuery query,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<OnboardingTask?> GetByIdAsync(
        long taskId,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken);

    Task<bool> SaveTransitionAsync(
        OnboardingTask task,
        OnboardingTaskStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    Task<bool> SaveAssignmentAsync(
        OnboardingTask task,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
