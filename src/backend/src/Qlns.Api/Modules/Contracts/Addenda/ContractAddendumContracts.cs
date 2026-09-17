using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;

namespace Qlns.Api.Modules.Contracts.Addenda;

/// <summary>OpenAPI ContractAddendumWrite.</summary>
public sealed record ContractAddendumWriteRequest(
    [Required, MaxLength(ContractAddendum.AddendumNumberMaxLength)] string AddendumNumber,
    DateOnly EffectiveDate,
    [Required] JsonObject BeforeTerms,
    [Required] JsonObject AfterTerms,
    [Required, MaxLength(ContractAddendum.ReasonMaxLength)] string Reason)
{
    public ContractAddendumWrite ToWrite() => new(AddendumNumber, EffectiveDate, BeforeTerms, AfterTerms, Reason);
}

/// <summary>OpenAPI ReasonRequest (optional body of the addendum transition endpoint; mandatory content for cancel).</summary>
public sealed record ReasonRequest([MaxLength(1000)] string? Reason);

/// <summary>OpenAPI ContractAddendum. Deliberately excludes the internal storage object key.</summary>
public sealed record ContractAddendumResponse(
    long Id,
    long ContractId,
    string AddendumNumber,
    DateOnly EffectiveDate,
    JsonObject BeforeTerms,
    JsonObject AfterTerms,
    string Reason,
    string Status,
    long Version,
    long CreatedBy,
    long? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? SignedAt,
    bool SignedDocumentAvailable)
{
    public static ContractAddendumResponse From(ContractAddendum addendum) => new(
        addendum.Id,
        addendum.ContractId,
        addendum.AddendumNumber,
        addendum.EffectiveDate,
        addendum.BeforeTerms,
        addendum.AfterTerms,
        addendum.Reason,
        addendum.Status.ToContract(),
        addendum.Version,
        addendum.CreatedBy,
        addendum.ApprovedBy,
        addendum.ApprovedAt,
        addendum.SignedAt,
        addendum.SignedDocumentAvailable);
}
