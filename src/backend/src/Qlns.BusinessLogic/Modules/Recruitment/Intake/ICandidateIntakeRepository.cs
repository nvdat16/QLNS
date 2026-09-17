using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Persistence contract for résumé intakes, candidates and the application created on confirmation.
/// Reads apply the actor's data scope through the job posting's department (404 outside scope); writes pair the
/// business change with its audit row in one transaction.
/// </summary>
public interface ICandidateIntakeRepository
{
    /// <summary>The requisition when it exists and its department is in the actor's data scope.</summary>
    Task<Requisition?> GetVisibleRequisitionAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>The intake when it exists and its requisition is in the actor's data scope.</summary>
    Task<CandidateIntake?> GetByIntakeIdAsync(Guid intakeId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Candidates by id, in id order; unknown ids are skipped.</summary>
    Task<IReadOnlyList<Candidate>> GetCandidatesAsync(IReadOnlyCollection<long> candidateIds, CancellationToken cancellationToken);

    /// <summary>Candidates sharing the normalised e-mail or (when given) the normalised phone, in id order.</summary>
    Task<IReadOnlyList<Candidate>> FindDuplicatesAsync(
        string normalizedEmail,
        string? normalizedPhone,
        CancellationToken cancellationToken);

    Task<bool> ApplicationExistsAsync(long candidateId, long requisitionId, CancellationToken cancellationToken);

    /// <summary>The application created by a completed intake together with its candidate (idempotent replay), or null.</summary>
    Task<IntakeConfirmation?> GetConfirmationAsync(CandidateIntake intake, CancellationToken cancellationToken);

    /// <summary>Inserts the intake row and its <c>recruitment.intake.start</c> audit row in one transaction; returns the persisted intake.</summary>
    Task<CandidateIntake> InsertAsync(CandidateIntake intake, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Persists <c>duplicate_review</c> and the duplicate ids with an audit row in one transaction.</summary>
    Task SaveDuplicateReviewAsync(
        CandidateIntake intake,
        CoreHrActor actor,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);

    /// <summary>
    /// One transaction: insert the candidate (transient, id 0) or conditionally update the linked candidate
    /// (<paramref name="expectedCandidateVersion"/>), create the application in <c>sourced_applied</c> with its first
    /// stage event, complete the intake and write the audit row. Returns null when the candidate update lost its
    /// version race; throws <see cref="CoreHrBusinessRuleException"/> (<c>recruitment.application.already_exists</c>)
    /// when the candidate already applied to the posting.
    /// </summary>
    Task<IntakeConfirmation?> ConfirmAsync(
        CandidateIntake intake,
        Candidate candidate,
        long? expectedCandidateVersion,
        string source,
        DateTimeOffset now,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
