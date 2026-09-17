using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.Api.Modules.Recruitment.Offers;

/// <summary>OpenAPI OfferWrite. Amount signs, currency pattern, employment type and date rules are validated by the domain (422).</summary>
public sealed record OfferWriteRequest(
    long ApplicationId,
    decimal BaseSalary,
    decimal? BonusAmount,
    decimal? AllowanceAmount,
    [MaxLength(3)] string? Currency,
    [Required, MaxLength(50)] string EmploymentType,
    DateOnly StartDate,
    DateOnly ExpirationDate,
    [Required, MaxLength(Offer.TemplateVersionMaxLength)] string TemplateVersion)
{
    public OfferWrite ToWrite() => new(
        ApplicationId,
        BaseSalary,
        BonusAmount,
        AllowanceAmount,
        Currency,
        EmploymentType,
        StartDate,
        ExpirationDate,
        TemplateVersion);
}

/// <summary>OpenAPI OfferAction (optional body of the transition endpoint): <c>reason</c> for cancel, <c>expirationDate</c> for extend.</summary>
public sealed record OfferActionRequest(
    [MaxLength(Offer.ReasonMaxLength)] string? Reason,
    DateOnly? ExpirationDate);

/// <summary>OpenAPI OfferResponse — the candidate's decision (named after its role to avoid clashing with the Offer DTO).</summary>
public sealed record OfferDecisionRequest(
    [Required, MaxLength(10)] string Decision,
    [MaxLength(Offer.ReasonMaxLength)] string? Reason);

/// <summary>OpenAPI Offer.</summary>
public sealed record OfferResponse(
    long Id,
    long ApplicationId,
    decimal BaseSalary,
    decimal? BonusAmount,
    decimal? AllowanceAmount,
    string Currency,
    string EmploymentType,
    DateOnly StartDate,
    DateOnly ExpirationDate,
    string TemplateVersion,
    string Status,
    bool DocumentAvailable,
    long? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? SentAt,
    DateTimeOffset? RespondedAt,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static OfferResponse From(Offer offer) => new(
        offer.Id,
        offer.ApplicationId,
        offer.BaseSalary,
        offer.BonusAmount,
        offer.AllowanceAmount,
        offer.Currency,
        offer.EmploymentType,
        offer.StartDate,
        offer.ExpirationDate,
        offer.TemplateVersion,
        offer.Status.ToContract(),
        offer.HasDocument,
        offer.ApprovedBy,
        offer.ApprovedAt,
        offer.SentAt,
        offer.RespondedAt,
        offer.Version,
        offer.CreatedAt,
        offer.UpdatedAt);
}

/// <summary>OpenAPI OfferResponseResult.</summary>
public sealed record OfferDecisionResultResponse(
    long OfferId,
    string Status,
    long? EmployeeId,
    long? InitialContractId,
    IReadOnlyList<long> OnboardingTaskIds,
    bool Replayed)
{
    public static OfferDecisionResultResponse From(OfferResponseResult result) => new(
        result.OfferId,
        result.Status.ToContract(),
        result.EmployeeId,
        result.InitialContractId,
        result.OnboardingTaskIds,
        result.Replayed);
}
