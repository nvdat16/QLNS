using System.Text.Json.Nodes;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>Client payload of OpenAPI ContractAddendumWrite. Validated by <see cref="ContractAddendum.CreateDraft"/>.</summary>
public sealed record ContractAddendumWrite(
    string? AddendumNumber,
    DateOnly EffectiveDate,
    JsonObject? BeforeTerms,
    JsonObject? AfterTerms,
    string? Reason);
