namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>
/// Client payload of RequisitionWrite (OpenAPI). <see cref="EmploymentType"/> is the contract string; it is
/// validated by <see cref="Requisition"/> together with the other semantic rules (422).
/// </summary>
public sealed record RequisitionWrite(
    string? Title,
    long DepartmentId,
    long? PositionId,
    string? Description,
    string? Requirements,
    string? Location,
    string? EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    int TargetHeadcount,
    DateOnly? ClosingDate);
