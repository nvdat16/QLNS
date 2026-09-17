using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Offboarding;

/// <summary>
/// EMP-07.1 / EMP-07.2 case endpoints: search, open, read, run the approve | start | complete | cancel workflow and
/// list the checklist of a case. The workflow route parameter is named <c>transition</c> because attribute routes may
/// not declare a parameter called <c>action</c>; the URL shape is unchanged.
/// </summary>
[Route("api/v1/offboarding/cases")]
public sealed class OffboardingCasesController(
    OffboardingCaseService caseService,
    OffboardingTaskService taskService) : CoreHrControllerBase
{
    private const string CasesPath = "/api/v1/offboarding/cases";

    [HttpGet(Name = "listOffboardingCases")]
    [Authorize(Policy = OffboardingPolicies.Read)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? status = null,
        [FromQuery] long? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return Task.FromResult<IActionResult>(InvalidPage());
        }

        if (departmentId is <= 0)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("departmentId", "departmentId must be a positive identifier."));
        }

        OffboardingCaseStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!OffboardingCaseStatusNames.TryParseContract(status, out var parsed))
            {
                return Task.FromResult<IActionResult>(ValidationProblemResult("status", "Unknown offboarding status. Use draft, pending_approval, approved, in_progress, completed or cancelled."));
            }

            statusFilter = parsed;
        }

        var query = new OffboardingCaseSearchQuery(statusFilter, departmentId, pageRequest);

        return ExecuteAsync(async actor =>
        {
            var result = await caseService.SearchAsync(query, actor, cancellationToken);
            return Ok(ToPage(result, OffboardingCaseResponse.From));
        });
    }

    [HttpPost(Name = "createOffboardingCase")]
    [Authorize(Policy = OffboardingPolicies.Write)]
    public Task<IActionResult> Create(
        [FromBody] OffboardingCaseWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var view = await caseService.CreateAsync(
                new CreateOffboardingCaseCommand(request.ToWrite(), actor),
                cancellationToken);

            SetETag(view.Case.Version);
            return Created(
                $"{CasesPath}/{view.Case.Id.ToString(CultureInfo.InvariantCulture)}",
                OffboardingCaseResponse.From(view));
        });

    [HttpGet("{caseId:long:min(1)}", Name = "getOffboardingCase")]
    [Authorize(Policy = OffboardingPolicies.Read)]
    public Task<IActionResult> Get(long caseId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var view = await caseService.GetAsync(caseId, actor, cancellationToken);
            SetETag(view.Case.Version);
            return Ok(OffboardingCaseResponse.From(view));
        });

    [HttpPost("{caseId:long:min(1)}/{transition:regex(^(approve|start|complete|cancel)$)}", Name = "transitionOffboardingCase")]
    [Authorize(Policy = OffboardingPolicies.Write)]
    public Task<IActionResult> Transition(
        long caseId,
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

            if (!OffboardingCaseActionNames.TryParseContract(transition, out var action))
            {
                return ValidationProblemResult("action", "action must be one of approve, start, complete, cancel.");
            }

            var view = await caseService.TransitionAsync(
                new TransitionOffboardingCaseCommand(caseId, action, expectedVersion, request?.Reason, actor),
                cancellationToken);

            SetETag(view.Case.Version);
            return Ok(OffboardingCaseResponse.From(view));
        });

    [HttpGet("{caseId:long:min(1)}/tasks", Name = "listOffboardingTasks")]
    [Authorize(Policy = OffboardingPolicies.Read)]
    public Task<IActionResult> ListTasks(
        long caseId,
        [FromQuery] string? category = null,
        [FromQuery] bool? blockingOnly = null,
        CancellationToken cancellationToken = default)
    {
        OffboardingTaskCategory? categoryFilter = null;
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!OffboardingTaskCategoryNames.TryParseContract(category, out var parsed))
            {
                return Task.FromResult<IActionResult>(ValidationProblemResult("category", "Unknown task category. Use it, admin, hr, manager or finance."));
            }

            categoryFilter = parsed;
        }

        var filter = new OffboardingTaskFilter(categoryFilter, blockingOnly);

        return ExecuteAsync(async actor =>
        {
            var tasks = await taskService.ListAsync(caseId, filter, actor, cancellationToken);
            return Ok(tasks.Select(OffboardingTaskResponse.From).ToList());
        });
    }
}
