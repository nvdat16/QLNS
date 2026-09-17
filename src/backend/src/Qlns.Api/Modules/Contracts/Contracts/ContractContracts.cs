using System.ComponentModel.DataAnnotations;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;

namespace Qlns.Api.Modules.Contracts.Contracts;

/// <summary>
/// OpenAPI ContractWrite. Missing <c>currency</c> defaults to VND and missing <c>isPrimary</c> to true, as documented.
/// Only shape (required, length) is annotated; ranges and date rules are semantic and answered 422 by the domain.
/// </summary>
public sealed record ContractWriteRequest(
    long EmployeeId,
    [Required, MaxLength(Contract.ContractNumberMaxLength)] string ContractNumber,
    [Required, MaxLength(40)] string ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal Salary,
    [MaxLength(3)] string? Currency = Contract.DefaultCurrency,
    int? NoticePeriodDays = null,
    bool IsPrimary = true)
{
    public ContractWrite ToWrite() => new(
        EmployeeId,
        ContractNumber,
        ContractType,
        StartDate,
        EndDate,
        Salary,
        Currency,
        NoticePeriodDays,
        IsPrimary);
}

/// <summary>OpenAPI ContractAction (optional body of the transition endpoint).</summary>
public sealed record ContractActionRequest(
    [MaxLength(1000)] string? Reason,
    DateTimeOffset? SignedAt,
    bool AllowPrimaryOverlap = false)
{
    public ContractActionOptions ToOptions() => new(Reason, SignedAt, AllowPrimaryOverlap);
}

/// <summary>OpenAPI Contract. Deliberately excludes the internal storage object key.</summary>
public sealed record ContractResponse(
    long Id,
    long EmployeeId,
    string ContractNumber,
    string ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal Salary,
    string Currency,
    int? NoticePeriodDays,
    bool IsPrimary,
    string Status,
    bool SignedDocumentAvailable,
    DateTimeOffset? SignedAt,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static ContractResponse From(Contract contract) => new(
        contract.Id,
        contract.EmployeeId,
        contract.ContractNumber,
        contract.ContractType.ToContract(),
        contract.StartDate,
        contract.EndDate,
        contract.Salary,
        contract.Currency,
        contract.NoticePeriodDays,
        contract.IsPrimary,
        contract.Status.ToContract(),
        contract.SignedDocumentAvailable,
        contract.SignedAt,
        contract.Version,
        contract.CreatedAt,
        contract.UpdatedAt);
}

/// <summary>OpenAPI ExpiringContract: a Contract plus days remaining and the badge colour.</summary>
public sealed record ExpiringContractResponse(
    long Id,
    long EmployeeId,
    string ContractNumber,
    string ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal Salary,
    string Currency,
    int? NoticePeriodDays,
    bool IsPrimary,
    string Status,
    bool SignedDocumentAvailable,
    DateTimeOffset? SignedAt,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int DaysRemaining,
    string AlertLevel)
{
    public static ExpiringContractResponse From(ExpiringContract expiring)
    {
        var contract = expiring.Contract;
        return new ExpiringContractResponse(
            contract.Id,
            contract.EmployeeId,
            contract.ContractNumber,
            contract.ContractType.ToContract(),
            contract.StartDate,
            contract.EndDate,
            contract.Salary,
            contract.Currency,
            contract.NoticePeriodDays,
            contract.IsPrimary,
            contract.Status.ToContract(),
            contract.SignedDocumentAvailable,
            contract.SignedAt,
            contract.Version,
            contract.CreatedAt,
            contract.UpdatedAt,
            expiring.DaysRemaining,
            expiring.AlertLevel.ToContract());
    }
}

/// <summary>OpenAPI ExpiringContractPage.</summary>
public sealed record ExpiringContractPageResponse(
    IReadOnlyList<ExpiringContractResponse> Items,
    PageMetadataResponse Page,
    DateOnly AsOf);
