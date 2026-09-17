using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>Payload of <c>POST /recruitment/offers</c> (OpenAPI OfferWrite). Validated by <see cref="Offer.Draft"/>.</summary>
public sealed record OfferWrite(
    long ApplicationId,
    decimal BaseSalary,
    decimal? BonusAmount,
    decimal? AllowanceAmount,
    string? Currency,
    string? EmploymentType,
    DateOnly StartDate,
    DateOnly ExpirationDate,
    string? TemplateVersion);

public sealed record CreateOfferCommand(OfferWrite Write, CoreHrActor Actor);

/// <summary>Optional body of the transition endpoint (OpenAPI OfferAction): <c>reason</c> for cancel, <c>expirationDate</c> for extend.</summary>
public sealed record TransitionOfferCommand(
    long OfferId,
    OfferAction Action,
    long ExpectedVersion,
    string? Reason,
    DateOnly? ExpirationDate,
    CoreHrActor Actor);

/// <summary>Candidate response (OpenAPI OfferResponse). The token was already validated by the presentation layer.</summary>
public sealed record RespondToOfferCommand(
    long OfferId,
    OfferDecision Decision,
    string? Reason,
    string CorrelationId);

/// <summary>Filters of <c>GET /recruitment/offers</c>. Data scope is applied server-side from the actor.</summary>
public sealed record OfferSearchQuery(long? ApplicationId, OfferStatus? Status, PageRequest Page);

/// <summary>Immutable copy of the workflow fields before a mutation; the audit "before" payload and the expected version.</summary>
public sealed record OfferSnapshot(OfferStatus Status, DateOnly ExpirationDate, long Version);
