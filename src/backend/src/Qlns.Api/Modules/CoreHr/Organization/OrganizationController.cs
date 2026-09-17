using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Organization;

namespace Qlns.Api.Modules.CoreHr.Organization;

/// <summary>Organization tag of the OpenAPI contract: chart, departments and positions (EMP-02.1).</summary>
[Route("api/v1/organization")]
public sealed class OrganizationController(OrganizationService service) : CoreHrControllerBase
{
    private const string DepartmentsPath = "/api/v1/organization/departments";
    private const string PositionsPath = "/api/v1/organization/positions";

    [HttpGet("chart", Name = "getOrganizationChart")]
    [Authorize(Policy = CoreHrPolicies.OrganizationRead)]
    public Task<IActionResult> GetChart(
        [FromQuery] long? rootDepartmentId,
        [FromQuery] int depth = DepartmentHierarchy.MaxDepth,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async actor =>
        {
            if (rootDepartmentId is <= 0)
            {
                return ValidationProblemResult("rootDepartmentId", "rootDepartmentId must be a positive identifier.");
            }

            var nodes = await service.GetChartAsync(rootDepartmentId, depth, actor, cancellationToken);
            return Ok(nodes.Select(ToResponse).ToList());
        });

    [HttpGet("departments", Name = "listDepartments")]
    [Authorize(Policy = CoreHrPolicies.OrganizationRead)]
    public Task<IActionResult> ListDepartments(CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var departments = await service.ListDepartmentsAsync(actor, cancellationToken);
            return Ok(departments.Select(ToResponse).ToList());
        });

    [HttpPost("departments", Name = "createDepartment")]
    [Authorize(Policy = CoreHrPolicies.OrganizationManage)]
    public Task<IActionResult> CreateDepartment(
        [FromBody] DepartmentWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.CreateDepartmentAsync(ToWrite(request), actor, cancellationToken);
            SetETag(created.Department.Version);
            return Created(
                $"{DepartmentsPath}/{created.Department.Id.ToString(CultureInfo.InvariantCulture)}",
                ToResponse(created));
        });

    [HttpPut("departments/{departmentId:long:min(1)}", Name = "replaceDepartment")]
    [Authorize(Policy = CoreHrPolicies.OrganizationManage)]
    public Task<IActionResult> ReplaceDepartment(
        long departmentId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] DepartmentWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            var updated = await service.ReplaceDepartmentAsync(
                departmentId, expectedVersion, ToWrite(request), actor, cancellationToken);
            SetETag(updated.Department.Version);
            return Ok(ToResponse(updated));
        });

    [HttpDelete("departments/{departmentId:long:min(1)}", Name = "deleteDepartment")]
    [Authorize(Policy = CoreHrPolicies.OrganizationManage)]
    public Task<IActionResult> DeleteDepartment(
        long departmentId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            await service.DeleteDepartmentAsync(departmentId, expectedVersion, actor, cancellationToken);
            return NoContent();
        });

    [HttpGet("positions", Name = "listPositions")]
    [Authorize(Policy = CoreHrPolicies.OrganizationRead)]
    public Task<IActionResult> ListPositions(CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var positions = await service.ListPositionsAsync(actor, cancellationToken);
            return Ok(positions.Select(ToResponse).ToList());
        });

    [HttpPost("positions", Name = "createPosition")]
    [Authorize(Policy = CoreHrPolicies.OrganizationManage)]
    public Task<IActionResult> CreatePosition(
        [FromBody] PositionWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.CreatePositionAsync(ToWrite(request), actor, cancellationToken);
            SetETag(created.Version);
            return Created(
                $"{PositionsPath}/{created.Id.ToString(CultureInfo.InvariantCulture)}",
                ToResponse(created));
        });

    [HttpPut("positions/{positionId:long:min(1)}", Name = "replacePosition")]
    [Authorize(Policy = CoreHrPolicies.OrganizationManage)]
    public Task<IActionResult> ReplacePosition(
        long positionId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] PositionWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            var updated = await service.ReplacePositionAsync(
                positionId, expectedVersion, ToWrite(request), actor, cancellationToken);
            SetETag(updated.Version);
            return Ok(ToResponse(updated));
        });

    private static DepartmentWrite ToWrite(DepartmentWriteRequest request) => new(
        request.Code,
        request.Name,
        request.ParentDepartmentId,
        request.CostCenter,
        request.Description);

    private static PositionWrite ToWrite(PositionWriteRequest request) => new(
        request.Code,
        request.Name,
        request.Level,
        request.Description);

    // manager is always null: departments has no manager_id column yet (see OrganizationNode remarks).
    private static DepartmentResponse ToResponse(DepartmentDetail detail) => new(
        detail.Department.Id,
        detail.Department.Code,
        detail.Department.Name,
        detail.Department.ParentDepartmentId,
        detail.Department.CostCenter,
        detail.Department.Description,
        Manager: null,
        detail.Headcount,
        detail.Department.Version);

    private static OrganizationNodeResponse ToResponse(OrganizationNode node) => new(
        node.Department.Id,
        node.Department.Code,
        node.Department.Name,
        node.Department.ParentDepartmentId,
        node.Department.CostCenter,
        node.Department.Description,
        Manager: null,
        node.Headcount,
        node.Department.Version,
        node.Children.Select(ToResponse).ToList());

    private static PositionResponse ToResponse(Position position) => new(
        position.Id,
        position.Code,
        position.Name,
        position.Level,
        position.Description,
        position.Version,
        position.CreatedAt,
        position.UpdatedAt);
}

/// <summary>OpenAPI <c>DepartmentWrite</c>. Semantic rules (code pattern, parent existence) are checked by the service.</summary>
public sealed record DepartmentWriteRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(255)] string Name,
    long? ParentDepartmentId,
    [MaxLength(100)] string? CostCenter,
    [MaxLength(5000)] string? Description);

/// <summary>OpenAPI <c>PositionWrite</c>.</summary>
public sealed record PositionWriteRequest(
    [Required, MaxLength(50)] string Code,
    [Required, MaxLength(255)] string Name,
    [MaxLength(50)] string? Level,
    [MaxLength(5000)] string? Description);

/// <summary>OpenAPI <c>Department</c>. <c>Manager</c> is reserved for EmployeeSummary once departments.manager_id exists.</summary>
public sealed record DepartmentResponse(
    long Id,
    string Code,
    string Name,
    long? ParentDepartmentId,
    string? CostCenter,
    string? Description,
    object? Manager,
    int Headcount,
    long Version);

/// <summary>OpenAPI <c>OrganizationNode</c>: a Department plus its children.</summary>
public sealed record OrganizationNodeResponse(
    long Id,
    string Code,
    string Name,
    long? ParentDepartmentId,
    string? CostCenter,
    string? Description,
    object? Manager,
    int Headcount,
    long Version,
    IReadOnlyList<OrganizationNodeResponse> Children);

/// <summary>OpenAPI <c>Position</c>.</summary>
public sealed record PositionResponse(
    long Id,
    string Code,
    string Name,
    string? Level,
    string? Description,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
