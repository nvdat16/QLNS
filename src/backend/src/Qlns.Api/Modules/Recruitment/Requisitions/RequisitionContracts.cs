using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.Api.Modules.Recruitment.Requisitions;

/// <summary>OpenAPI RequisitionWrite. Shape is checked here; ranges, enum values and dates are validated by the domain (422).</summary>
public sealed record RequisitionWriteRequest(
    [Required, MaxLength(Requisition.TitleMaxLength)] string Title,
    long DepartmentId,
    long? PositionId,
    [MaxLength(Requisition.LongTextMaxLength)] string? Description,
    [MaxLength(Requisition.LongTextMaxLength)] string? Requirements,
    [MaxLength(Requisition.LocationMaxLength)] string? Location,
    [Required, MaxLength(30)] string EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    int TargetHeadcount,
    DateOnly? ClosingDate)
{
    public RequisitionWrite ToWrite() => new(
        Title,
        DepartmentId,
        PositionId,
        Description,
        Requirements,
        Location,
        EmploymentType,
        SalaryMin,
        SalaryMax,
        TargetHeadcount,
        ClosingDate);
}

/// <summary>OpenAPI RequisitionAction (optional body of the transition endpoint). Reason is mandatory for reject and cancel (enforced by the domain).</summary>
public sealed record RequisitionActionRequest([MaxLength(Requisition.ReasonMaxLength)] string? Reason);

/// <summary>OpenAPI Requisition.</summary>
public sealed record RequisitionResponse(
    long Id,
    string JobCode,
    string Title,
    long DepartmentId,
    long? PositionId,
    string? Description,
    string? Requirements,
    string? Location,
    string EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    int TargetHeadcount,
    DateOnly? ClosingDate,
    string Status,
    DateTimeOffset? PublishedAt,
    long CreatedBy,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static RequisitionResponse From(Requisition requisition) => new(
        requisition.Id,
        requisition.JobCode,
        requisition.Title,
        requisition.DepartmentId,
        requisition.PositionId,
        requisition.Description,
        requisition.Requirements,
        requisition.Location,
        requisition.EmploymentType.ToContract(),
        requisition.SalaryMin,
        requisition.SalaryMax,
        requisition.TargetHeadcount,
        requisition.ClosingDate,
        requisition.Status.ToContract(),
        requisition.PublishedAt,
        requisition.CreatedBy,
        requisition.Version,
        requisition.CreatedAt,
        requisition.UpdatedAt);
}
