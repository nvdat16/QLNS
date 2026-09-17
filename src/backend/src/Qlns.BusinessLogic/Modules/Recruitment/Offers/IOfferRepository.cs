using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>
/// Persistence contract for offers and the acceptance handoff. Scoped reads join applications → job_postings and
/// apply the actor's department scope; candidate-facing reads are unscoped because the token is the authorization.
/// Every write pairs the business change with its audit (and outbox) rows in one transaction.
/// </summary>
public interface IOfferRepository
{
    Task<PagedResult<Offer>> SearchAsync(OfferSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Offer by id when its job posting department is in the actor's scope; otherwise null.</summary>
    Task<Offer?> GetByIdAsync(long offerId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>Offer by id without data scope — for the candidate response flow (authorized by X-Offer-Token).</summary>
    Task<Offer?> GetForCandidateAsync(long offerId, CancellationToken cancellationToken);

    /// <summary>Application with its job posting department, or null when missing or outside the actor's scope.</summary>
    Task<OfferApplication?> GetApplicationAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>True when the application already has an offer in draft, approved, sent or accepted.</summary>
    Task<bool> HasOpenOfferAsync(long applicationId, CancellationToken cancellationToken);

    /// <summary>Inserts the draft and its audit row; null when <c>ux_offers_one_open_per_application</c> rejected it.</summary>
    Task<Offer?> InsertAsync(Offer offer, CoreHrActor actor, CancellationToken cancellationToken);

    /// <summary>
    /// Conditional update on <c>id AND version = before.Version</c> plus audit row. For <see cref="OfferTransition.Send"/> and
    /// <see cref="OfferTransition.Extend"/> the outbox notification carrying <paramref name="responseToken"/> is added too.
    /// False, with rollback, when no row matched.
    /// </summary>
    Task<bool> SaveTransitionAsync(
        Offer offer,
        OfferTransition transition,
        OfferSnapshot before,
        string? reason,
        string? responseToken,
        CoreHrActor actor,
        CancellationToken cancellationToken);

    /// <summary>Offers still <c>sent</c> whose expiration date is strictly before <paramref name="today"/>.</summary>
    Task<IReadOnlyList<Offer>> ListExpiredSentAsync(DateOnly today, CancellationToken cancellationToken);

    /// <summary>Application, candidate and job posting of the offer being accepted; null when the application is missing.</summary>
    Task<OfferHandoffContext?> GetHandoffContextAsync(long applicationId, CancellationToken cancellationToken);

    /// <summary>Employee already created from the application (by <c>source_application_id</c>) with its draft probation contract and tasks.</summary>
    Task<OfferHandoffResult?> FindHandoffAsync(long applicationId, CancellationToken cancellationToken);

    /// <summary>
    /// One transaction (sequence diagram §4): offer → accepted, application → hired_ready + stage event, employee, contract,
    /// onboarding tasks, audit rows and the <c>recruitment.offer.accepted</c> outbox message. Null, with rollback, when the
    /// offer or application version no longer matches or another request already created the employee.
    /// </summary>
    Task<OfferHandoffResult?> SaveAcceptanceAsync(
        Offer offer,
        OfferSnapshot before,
        OfferHandoffPlan plan,
        CoreHrActor actor,
        CancellationToken cancellationToken);
}
