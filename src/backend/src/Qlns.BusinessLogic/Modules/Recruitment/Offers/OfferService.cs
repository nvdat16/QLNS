using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>
/// REC-06.1 / REC-06.2 use cases: search and read offers, draft an offer, run the approve / send / extend / cancel
/// workflow, record the candidate's response (with the idempotent employee handoff on accept) and expire due offers.
/// A <c>sent</c> offer past its expiration date is expired lazily whenever it is loaded, before any other rule runs.
/// </summary>
public sealed class OfferService(
    IOfferRepository repository,
    IOfferResponseTokenService responseTokens,
    TimeProvider timeProvider)
{
    public const string StageNotOfferCode = "recruitment.offer.stage_not_offer";
    public const string AlreadyOpenCode = "recruitment.offer.already_open";
    public const string NotOpenCode = "recruitment.offer.not_open";
    public const string WriteForbiddenCode = "recruitment.offer.write_forbidden";
    public const string ApproveForbiddenCode = "recruitment.offer.approve_forbidden";

    private const string ResourceName = "Offer";
    private const string ConflictResource = "offer";

    public Task<PagedResult<Offer>> SearchAsync(OfferSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);
        return repository.SearchAsync(query, actor, cancellationToken);
    }

    public async Task<Offer> GetAsync(long offerId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        return await LoadScopedAsync(offerId, actor, cancellationToken);
    }

    public async Task<Offer> CreateAsync(CreateOfferCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var application = await repository.GetApplicationAsync(command.Write.ApplicationId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException("Application", command.Write.ApplicationId);

        RequireWrite(actor);

        if (application.Stage != OfferApplicationStages.OfferLetter)
        {
            throw new CoreHrBusinessRuleException(
                StageNotOfferCode,
                $"Application {application.Id} is in stage {application.Stage}; offers can be drafted only in offer_letter.")
            {
                Details = new Dictionary<string, object?> { ["currentStage"] = application.Stage }
            };
        }

        if (await repository.HasOpenOfferAsync(application.Id, cancellationToken))
        {
            throw AlreadyOpen(application.Id);
        }

        var now = timeProvider.GetUtcNow();
        var offer = Offer.Draft(command.Write, Today(now), now);

        return await repository.InsertAsync(offer, actor, cancellationToken)
            ?? throw AlreadyOpen(application.Id);
    }

    public async Task<Offer> TransitionAsync(TransitionOfferCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var offer = await LoadScopedAsync(command.OfferId, actor, cancellationToken);

        if (offer.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        var now = timeProvider.GetUtcNow();
        var before = offer.Snapshot();
        OfferTransition transition;
        string? reason = null;
        string? responseToken = null;

        switch (command.Action)
        {
            case OfferAction.Approve:
                RequireApprove(actor);
                offer.Approve(actor.UserId, now);
                transition = OfferTransition.Approve;
                break;
            case OfferAction.Send:
                RequireWrite(actor);
                offer.Send(now);
                responseToken = IssueToken(offer);
                transition = OfferTransition.Send;
                break;
            case OfferAction.Extend:
                RequireApprove(actor);
                offer.Extend(command.ExpirationDate, Today(now), now);
                responseToken = IssueToken(offer);
                transition = OfferTransition.Extend;
                break;
            case OfferAction.Cancel:
                RequireWrite(actor);
                offer.Cancel(command.Reason, now);
                reason = command.Reason?.Trim();
                transition = OfferTransition.Cancel;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unknown offer action.");
        }

        var saved = await repository.SaveTransitionAsync(offer, transition, before, reason, responseToken, actor, cancellationToken);
        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return offer;
    }

    /// <summary>
    /// Candidate decision (sequence diagram §4). Accept runs the handoff in one transaction and is idempotent: an
    /// already accepted offer, or an employee already linked to the application, replays the existing identifiers.
    /// Decline replays as well when the offer was already declined.
    /// </summary>
    public async Task<OfferResponseResult> RespondAsync(RespondToOfferCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = CandidateActor(command.CorrelationId);

        var offer = await LoadForCandidateAsync(command.OfferId, actor, cancellationToken);

        return command.Decision switch
        {
            OfferDecision.Accept => await AcceptAsync(offer, actor, cancellationToken),
            OfferDecision.Decline => await DeclineAsync(offer, command.Reason, actor, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(command), "Unknown offer decision.")
        };
    }

    /// <summary>Worker entry point: expires every <c>sent</c> offer whose expiration date is before <paramref name="today"/>.</summary>
    public async Task<ExpireDueOffersResult> ExpireDueOffersAsync(DateOnly today, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var due = await repository.ListExpiredSentAsync(today, cancellationToken);
        var expired = 0;
        var conflicted = new List<long>();

        foreach (var offer in due)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var before = offer.Snapshot();
            offer.Expire(timeProvider.GetUtcNow());
            if (await repository.SaveTransitionAsync(offer, OfferTransition.Expire, before, null, null, actor, cancellationToken))
            {
                expired++;
            }
            else
            {
                conflicted.Add(offer.Id);
            }
        }

        return new ExpireDueOffersResult(expired, conflicted);
    }

    private async Task<OfferResponseResult> AcceptAsync(Offer offer, CoreHrActor actor, CancellationToken cancellationToken)
    {
        if (offer.Status == OfferStatus.Accepted)
        {
            return await ReplayAcceptanceAsync(offer, cancellationToken)
                ?? throw new InvalidOperationException($"Offer {offer.Id} is accepted but no employee is linked to application {offer.ApplicationId}.");
        }

        RequireOpen(offer);

        var existing = await repository.FindHandoffAsync(offer.ApplicationId, cancellationToken);
        if (existing is not null)
        {
            return OfferResponseResult.Accepted(offer.Id, existing, replayed: true);
        }

        var context = await repository.GetHandoffContextAsync(offer.ApplicationId, cancellationToken)
            ?? throw new InvalidOperationException($"Application {offer.ApplicationId} of offer {offer.Id} no longer exists.");

        var before = offer.Snapshot();
        offer.Accept(timeProvider.GetUtcNow());
        var plan = OfferHandoff.Plan(offer, context);

        var handoff = await repository.SaveAcceptanceAsync(offer, before, plan, actor, cancellationToken);
        if (handoff is not null)
        {
            return OfferResponseResult.Accepted(offer.Id, handoff, replayed: false);
        }

        // Lost the race: a concurrent accept may already have created the employee (scenario 3 of REC-06.2).
        return await ReplayAcceptanceAsync(offer, cancellationToken)
            ?? throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    private async Task<OfferResponseResult> DeclineAsync(Offer offer, string? reason, CoreHrActor actor, CancellationToken cancellationToken)
    {
        if (offer.Status == OfferStatus.Declined)
        {
            return OfferResponseResult.Declined(offer.Id, replayed: true);
        }

        RequireOpen(offer);

        var before = offer.Snapshot();
        offer.Decline(timeProvider.GetUtcNow());

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var saved = await repository.SaveTransitionAsync(offer, OfferTransition.Decline, before, trimmedReason, null, actor, cancellationToken);
        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return OfferResponseResult.Declined(offer.Id, replayed: false);
    }

    private async Task<OfferResponseResult?> ReplayAcceptanceAsync(Offer offer, CancellationToken cancellationToken)
    {
        var existing = await repository.FindHandoffAsync(offer.ApplicationId, cancellationToken);
        return existing is null ? null : OfferResponseResult.Accepted(offer.Id, existing, replayed: true);
    }

    private void RequireOpen(Offer offer)
    {
        if (!offer.IsOpenForResponse(Today(timeProvider.GetUtcNow())))
        {
            throw new CoreHrBusinessRuleException(
                NotOpenCode,
                $"Offer {offer.Id} is {offer.Status.ToContract()} and no longer accepts a response.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = offer.Status.ToContract() }
            };
        }
    }

    private async Task<Offer> LoadScopedAsync(long offerId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var offer = await repository.GetByIdAsync(offerId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, offerId);

        return await ExpireIfDueAsync(offer, actor, () => repository.GetByIdAsync(offerId, actor, cancellationToken), cancellationToken);
    }

    private async Task<Offer> LoadForCandidateAsync(long offerId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var offer = await repository.GetForCandidateAsync(offerId, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, offerId);

        return await ExpireIfDueAsync(offer, actor, () => repository.GetForCandidateAsync(offerId, cancellationToken), cancellationToken);
    }

    /// <summary>Lazy expiry: persists sent → expired before the caller applies any other rule; reloads when the save lost a race.</summary>
    private async Task<Offer> ExpireIfDueAsync(
        Offer offer,
        CoreHrActor actor,
        Func<Task<Offer?>> reload,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        if (!offer.IsExpired(Today(now)))
        {
            return offer;
        }

        var before = offer.Snapshot();
        offer.Expire(now);
        if (await repository.SaveTransitionAsync(offer, OfferTransition.Expire, before, null, null, actor, cancellationToken))
        {
            return offer;
        }

        return await reload() ?? throw new CoreHrNotFoundException(ResourceName, offer.Id);
    }

    private string IssueToken(Offer offer) =>
        responseTokens.Issue(offer.Id, OfferResponseTokenLifetime.ExpiresAt(offer.ExpirationDate));

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);

    /// <summary>The unauthenticated candidate: no user id (audit actor null), no scope, no permissions.</summary>
    private static CoreHrActor CandidateActor(string correlationId) => new(
        UserId: 0,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Self,
        Permissions: new HashSet<string>(),
        CorrelationId: correlationId);

    private static CoreHrBusinessRuleException AlreadyOpen(long applicationId) => new(
        AlreadyOpenCode,
        $"Application {applicationId} already has an open offer (draft, approved, sent or accepted).")
    {
        Details = new Dictionary<string, object?> { ["applicationId"] = applicationId }
    };

    private static void RequireWrite(CoreHrActor actor)
    {
        if (!actor.HasPermission(OfferPermissions.Write))
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Drafting, sending and cancelling offers requires the recruitment.offer.write permission.");
        }
    }

    private static void RequireApprove(CoreHrActor actor)
    {
        if (!actor.HasPermission(OfferPermissions.Approve))
        {
            throw new CoreHrForbiddenException(
                ApproveForbiddenCode,
                "Approving or extending offers requires the recruitment.offer.approve permission.");
        }
    }
}
