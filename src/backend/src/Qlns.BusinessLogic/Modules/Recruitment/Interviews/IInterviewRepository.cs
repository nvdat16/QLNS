using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>
/// Persistence contract for interviews. Reads apply the actor's data scope (job posting department in scope,
/// or the actor sits on the panel); writes pair the business change with its audit and outbox rows in one transaction.
/// </summary>
public interface IInterviewRepository
{
    Task<PagedResult<Interview>> SearchAsync(InterviewSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken);

    Task<Interview?> GetByIdAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Application with its job posting department, or null when missing or outside the actor's scope.</summary>
    Task<InterviewApplication?> GetApplicationAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Subset of <paramref name="userIds"/> that exist and are active.</summary>
    Task<IReadOnlySet<long>> FindActiveUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken);

    /// <summary>
    /// Scheduled interviews overlapping <paramref name="slot"/> that share a panelist with <paramref name="panelUserIds"/>
    /// or use <paramref name="location"/>, excluding <paramref name="excludeInterviewId"/>.
    /// </summary>
    Task<IReadOnlyList<ScheduledInterviewSummary>> ListOverlappingScheduledAsync(
        InterviewSlot slot,
        IReadOnlyCollection<long> panelUserIds,
        string? location,
        long? excludeInterviewId,
        CancellationToken cancellationToken);

    /// <summary>Inserts interview, panelists, audit row and the <c>recruitment.interview.scheduled</c> outbox message; returns the persisted interview.</summary>
    Task<Interview> InsertAsync(Interview interview, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Conditional update on <c>id AND version = expectedVersion</c> plus audit (and outbox for reschedule/cancel); false when no row matched.</summary>
    Task<bool> SaveTransitionAsync(
        Interview interview,
        InterviewAction action,
        InterviewSnapshot before,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
