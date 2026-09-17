using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>
/// Persistence contract for scorecards. Interview lookups apply the same data scope as the Interviews feature
/// (job posting department in scope or actor on the panel). Rows are insert-only; the unique index
/// <c>ux_evaluation_interviewer_version</c> is the natural idempotency key.
/// </summary>
public interface IEvaluationRepository
{
    /// <summary>Interview status and panel, or null when missing or outside the actor's scope.</summary>
    Task<EvaluationInterview?> GetInterviewAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Every version of every evaluator for the interview, without policy filtering.</summary>
    Task<IReadOnlyList<Evaluation>> ListByInterviewAsync(long interviewId, CancellationToken cancellationToken);

    /// <summary>Evaluation by id when its interview is visible to the actor; otherwise null.</summary>
    Task<Evaluation?> GetByIdAsync(long evaluationId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Highest version the evaluator has for the interview, or null when none.</summary>
    Task<Evaluation?> GetLatestAsync(long interviewId, long evaluatorUserId, CancellationToken cancellationToken);

    /// <summary>Inserts the submission and its audit row in one transaction; null when the version already exists (lost race).</summary>
    Task<Evaluation?> InsertSubmissionAsync(Evaluation evaluation, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Inserts the unlock version and its audit row in one transaction; null when the version already exists (lost race).</summary>
    Task<Evaluation?> InsertUnlockedVersionAsync(Evaluation evaluation, int previousVersion, CoreHrActor actor, CancellationToken cancellationToken);
}
