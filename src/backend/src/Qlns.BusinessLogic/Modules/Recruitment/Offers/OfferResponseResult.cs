namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>OpenAPI OfferResponseResult: outcome of the candidate's decision, first time or replayed.</summary>
public sealed record OfferResponseResult(
    long OfferId,
    OfferStatus Status,
    long? EmployeeId,
    long? InitialContractId,
    IReadOnlyList<long> OnboardingTaskIds,
    bool Replayed)
{
    public static OfferResponseResult Accepted(long offerId, OfferHandoffResult handoff, bool replayed) =>
        new(offerId, OfferStatus.Accepted, handoff.EmployeeId, handoff.InitialContractId, handoff.OnboardingTaskIds, replayed);

    public static OfferResponseResult Declined(long offerId, bool replayed) =>
        new(offerId, OfferStatus.Declined, null, null, [], replayed);
}

/// <summary>Outcome of one expiry worker run: expired count and the ids that lost a version race.</summary>
public sealed record ExpireDueOffersResult(int Expired, IReadOnlyList<long> Conflicted);
