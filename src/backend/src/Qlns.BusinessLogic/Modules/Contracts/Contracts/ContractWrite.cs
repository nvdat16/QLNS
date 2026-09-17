namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// Client payload of OpenAPI ContractWrite, used by both create and full replace. Validated by
/// <see cref="Contract.CreateDraft"/> / <see cref="Contract.Replace"/>. A blank <see cref="Currency"/> means the
/// contract default <c>VND</c>.
/// </summary>
public sealed record ContractWrite(
    long EmployeeId,
    string? ContractNumber,
    string? ContractType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal Salary,
    string? Currency,
    int? NoticePeriodDays,
    bool IsPrimary);
