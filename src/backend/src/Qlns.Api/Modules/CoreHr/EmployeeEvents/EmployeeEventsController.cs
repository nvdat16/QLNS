using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

namespace Qlns.Api.Modules.CoreHr.EmployeeEvents;

/// <summary>EMP-04.1: movement history and draft proposals of one employee.</summary>
[Route("api/v1/employees/{employeeId:long:min(1)}/events")]
public sealed class EmployeeEventsController(EmployeeMovementService service) : CoreHrControllerBase
{
    [HttpGet(Name = "listEmployeeEvents")]
    [Authorize(Policy = CoreHrPolicies.EventRead)]
    public Task<IActionResult> List(
        long employeeId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async actor =>
        {
            if (!TryCreatePage(page, pageSize, out var pageRequest))
            {
                return InvalidPage();
            }

            var result = await service.ListAsync(employeeId, pageRequest, actor, cancellationToken);
            return Ok(ToPage(result, EmployeeEventResponse.From));
        });

    [HttpPost(Name = "createEmployeeEvent")]
    [Authorize(Policy = CoreHrPolicies.EventWrite)]
    public Task<IActionResult> Create(
        long employeeId,
        [FromBody] EmployeeEventWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.CreateAsync(
                new CreateEmployeeEventCommand(employeeId, request.ToWrite(), actor),
                cancellationToken);

            SetETag(created.Version);
            return Created(
                $"/api/v1/employee-events/{created.Id.ToString(CultureInfo.InvariantCulture)}",
                EmployeeEventResponse.From(created));
        });
}
