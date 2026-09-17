using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

namespace Qlns.Api.Modules.CoreHr.EmployeeEvents;

/// <summary>
/// EMP-04.1 workflow: <c>POST /api/v1/employee-events/{eventId}/{action}</c> with action submit | approve | cancel.
/// The route parameter is named <c>transition</c> because attribute routes may not declare a parameter called
/// <c>action</c>; the URL shape is unchanged.
/// </summary>
[Route("api/v1/employee-events")]
public sealed class EmployeeEventTransitionsController(EmployeeMovementService service) : CoreHrControllerBase
{
    [HttpPost("{eventId:long:min(1)}/{transition:regex(^(submit|approve|cancel)$)}", Name = "transitionEmployeeEvent")]
    [Authorize(Policy = CoreHrPolicies.EventWrite)]
    public Task<IActionResult> Transition(
        long eventId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReasonRequest? request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            if (!EmployeeEventActionNames.TryParseContract(transition, out var action))
            {
                return ValidationProblemResult("action", "action must be one of submit, approve, cancel.");
            }

            var updated = await service.TransitionAsync(
                new TransitionEmployeeEventCommand(eventId, action, expectedVersion, request?.Reason, actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(EmployeeEventResponse.From(updated));
        });
}
