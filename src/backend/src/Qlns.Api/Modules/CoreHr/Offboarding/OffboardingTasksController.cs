using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

namespace Qlns.Api.Modules.CoreHr.Offboarding;

/// <summary>
/// EMP-07.2 checklist workflow: <c>POST /api/v1/offboarding/tasks/{taskId}/{action}</c> with action start | complete | reopen.
/// Assignees outside the employee's department may work their own task but still need <c>corehr.offboarding.write</c>.
/// </summary>
[Route("api/v1/offboarding/tasks")]
public sealed class OffboardingTasksController(OffboardingTaskService service) : CoreHrControllerBase
{
    [HttpPost("{taskId:long:min(1)}/{transition:regex(^(start|complete|reopen)$)}", Name = "transitionOffboardingTask")]
    [Authorize(Policy = OffboardingPolicies.Write)]
    public Task<IActionResult> Transition(
        long taskId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] DecisionRequest? request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            if (!OffboardingTaskActionNames.TryParseContract(transition, out var action))
            {
                return ValidationProblemResult("action", "action must be one of start, complete, reopen.");
            }

            var task = await service.TransitionAsync(
                new TransitionOffboardingTaskCommand(taskId, action, expectedVersion, request?.Reason, actor),
                cancellationToken);

            SetETag(task.Version);
            return Ok(OffboardingTaskResponse.From(task));
        });
}
