using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Employees;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Employees;

[Route("api/v1/employees")]
public sealed class EmployeesController(EmployeeDirectoryService service) : CoreHrControllerBase
{
    private const int MaxSearchLength = 200;
    private const string FieldForbiddenCode = "corehr.employee.field_forbidden";

    private static readonly IReadOnlySet<string> PatchableFields = new HashSet<string>(StringComparer.Ordinal)
    {
        Employee.PersonalEmailField,
        Employee.PhoneField,
        Employee.TemporaryAddressField,
        Employee.EmergencyContactField
    };

    [HttpGet(Name = "listEmployees")]
    [Authorize(Policy = CoreHrPolicies.EmployeeRead)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] long? positionId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = EmployeeSort.Name,
        CancellationToken cancellationToken = default) => ExecuteAsync(async actor =>
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return InvalidPage();
        }

        if (search is { Length: > MaxSearchLength })
        {
            return ValidationProblemResult("search", $"search must be at most {MaxSearchLength} characters.");
        }

        if (departmentId is < 1)
        {
            return ValidationProblemResult("departmentId", "departmentId must be at least 1.");
        }

        if (positionId is < 1)
        {
            return ValidationProblemResult("positionId", "positionId must be at least 1.");
        }

        EmployeeStatus? statusFilter = null;
        if (status is not null)
        {
            if (!EmployeeStatusNames.TryParseContract(status, out var parsedStatus))
            {
                return ValidationProblemResult("status", "status must be one of probation, active, suspended, terminated.");
            }

            statusFilter = parsedStatus;
        }

        var result = await service.SearchAsync(
            new EmployeeSearchQuery(search, departmentId, positionId, statusFilter, EmployeeSort.Normalize(sort), pageRequest),
            actor,
            cancellationToken);

        return Ok(ToPage(result, ToSummary));
    });

    [HttpGet("{employeeId:long:min(1)}", Name = "getEmployee")]
    [Authorize(Policy = CoreHrPolicies.EmployeeRead)]
    public Task<IActionResult> Get(long employeeId, CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        var view = await service.GetAsync(employeeId, actor, cancellationToken);
        SetETag(view.Employee.Version);
        return Ok(ToDetail(view));
    });

    [HttpPatch("{employeeId:long:min(1)}/profile", Name = "updateEmployeePersonalProfile")]
    [Authorize(Policy = CoreHrPolicies.EmployeeProfileUpdate)]
    [Consumes("application/merge-patch+json")]
    public Task<IActionResult> UpdatePersonalProfile(
        long employeeId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return InvalidIfMatch();
        }

        if (body.ValueKind != JsonValueKind.Object)
        {
            return ProblemResult(400, "common.invalid_request", "Request body must be a JSON object",
                "PersonalProfilePatch is a merge-patch document with the properties personalEmail, phone, temporaryAddress and emergencyContact.");
        }

        // EMP-01.2 scenario 2: any attempt to change a work field through this endpoint is refused with 403.
        var forbiddenFields = body.EnumerateObject()
            .Select(property => property.Name)
            .Where(name => !PatchableFields.Contains(name))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (forbiddenFields.Count > 0)
        {
            return ProblemResult(403, FieldForbiddenCode, "Field cannot be changed here",
                $"The following fields cannot be changed through the personal profile: {string.Join(", ", forbiddenFields)}. " +
                "Only personalEmail, phone, temporaryAddress and emergencyContact are allowed; work fields change only through employee events.");
        }

        var patch = ReadPatch(body);
        var view = await service.UpdatePersonalProfileAsync(
            new UpdatePersonalProfileCommand(employeeId, expectedVersion, patch, actor),
            cancellationToken);

        SetETag(view.Employee.Version);
        return Ok(ToDetail(view));
    });

    /// <summary>Maps the merge-patch document to the domain patch; shape errors surface as 422 via ExecuteAsync.</summary>
    private static PersonalProfilePatch ReadPatch(JsonElement body)
    {
        var errors = new ValidationErrors();
        var patch = PersonalProfilePatch.Empty;

        foreach (var property in body.EnumerateObject())
        {
            switch (property.Name)
            {
                case Employee.PersonalEmailField:
                    patch = patch with { PersonalEmail = ReadString(property, errors) };
                    break;
                case Employee.PhoneField:
                    patch = patch with { Phone = ReadString(property, errors) };
                    break;
                case Employee.TemporaryAddressField:
                    patch = patch with { TemporaryAddress = ReadString(property, errors) };
                    break;
                case Employee.EmergencyContactField:
                    patch = patch with { EmergencyContact = ReadStringMap(property, errors) };
                    break;
            }
        }

        errors.ThrowIfAny();
        return patch;
    }

    private static Optional<string?> ReadString(JsonProperty property, ValidationErrors errors)
    {
        switch (property.Value.ValueKind)
        {
            case JsonValueKind.Null:
                return Optional<string?>.Of(null);
            case JsonValueKind.String:
                return Optional<string?>.Of(property.Value.GetString());
            default:
                errors.Add(property.Name, $"{property.Name} must be a string or null.");
                return Optional<string?>.Unset;
        }
    }

    private static Optional<IReadOnlyDictionary<string, string>?> ReadStringMap(JsonProperty property, ValidationErrors errors)
    {
        if (property.Value.ValueKind == JsonValueKind.Null)
        {
            return Optional<IReadOnlyDictionary<string, string>?>.Of(null);
        }

        if (property.Value.ValueKind != JsonValueKind.Object)
        {
            errors.Add(property.Name, $"{property.Name} must be an object of string values or null.");
            return Optional<IReadOnlyDictionary<string, string>?>.Unset;
        }

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        var valid = true;
        foreach (var entry in property.Value.EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.String)
            {
                errors.Add($"{property.Name}.{entry.Name}", "emergencyContact values must be strings.");
                valid = false;
                continue;
            }

            map[entry.Name] = entry.Value.GetString() ?? string.Empty;
        }

        return valid
            ? Optional<IReadOnlyDictionary<string, string>?>.Of(map)
            : Optional<IReadOnlyDictionary<string, string>?>.Unset;
    }

    private static EmployeeSummaryResponse ToSummary(Employee employee) => new(
        employee.Id,
        employee.EmployeeCode,
        employee.FirstName,
        employee.LastName,
        employee.WorkEmail,
        employee.DepartmentId,
        employee.PositionId,
        employee.ManagerId,
        employee.OfficeLocation,
        employee.Status.ToContract());

    private static EmployeeDetailResponse ToDetail(EmployeeView view)
    {
        var employee = view.Employee;
        var full = view.Visibility == EmployeeFieldVisibility.Full;

        return new EmployeeDetailResponse(
            employee.Id,
            employee.EmployeeCode,
            employee.FirstName,
            employee.LastName,
            employee.WorkEmail,
            employee.DepartmentId,
            employee.PositionId,
            employee.ManagerId,
            employee.OfficeLocation,
            employee.Status.ToContract(),
            PersonalEmail: full ? employee.PersonalEmail : null,
            Phone: full ? employee.Phone : null,
            DateOfBirth: full ? employee.DateOfBirth : null,
            Gender: full ? employee.Gender : null,
            PermanentAddress: full ? employee.PermanentAddress : null,
            TemporaryAddress: full ? employee.TemporaryAddress : null,
            EmergencyContact: full ? employee.EmergencyContact : null,
            employee.HireDate,
            employee.Version,
            employee.CreatedAt,
            employee.UpdatedAt);
    }
}

/// <summary>OpenAPI <c>EmployeeSummary</c>: public work fields only.</summary>
public sealed record EmployeeSummaryResponse(
    long Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? WorkEmail,
    long DepartmentId,
    long PositionId,
    long? ManagerId,
    string? OfficeLocation,
    string Status);

/// <summary>OpenAPI <c>EmployeeDetail</c>: personal fields are null when the actor's visibility is Public.</summary>
public sealed record EmployeeDetailResponse(
    long Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string? WorkEmail,
    long DepartmentId,
    long PositionId,
    long? ManagerId,
    string? OfficeLocation,
    string Status,
    string? PersonalEmail,
    string? Phone,
    DateOnly? DateOfBirth,
    string? Gender,
    string? PermanentAddress,
    string? TemporaryAddress,
    IReadOnlyDictionary<string, string>? EmergencyContact,
    DateOnly HireDate,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
