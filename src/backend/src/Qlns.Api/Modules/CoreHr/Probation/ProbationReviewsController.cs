using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Probation;

/// <summary>
/// EMP-06.1 search (overdue dashboard) and EMP-06.2 workflow:
/// <c>POST /api/v1/probation-reviews/{reviewId}/{action}</c> with action decide | cancel | unlock.
/// The route parameter is named <c>transition</c> because attribute routes may not declare a parameter called
/// <c>action</c>; the URL shape is unchanged.
/// </summary>
[Route("api/v1/probation-reviews")]
public sealed class ProbationReviewsController(ProbationReviewService service) : CoreHrControllerBase
{
    [HttpGet(Name = "listProbationReviews")]
    [Authorize(Policy = ProbationPolicies.Read)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? status = null,
        [FromQuery] bool? overdue = null,
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

        ProbationReviewStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!ProbationReviewStatusNames.TryParseContract(status, out var parsed))
            {
                return Task.FromResult<IActionResult>(ValidationProblemResult("status", "Unknown probation review status. Use pending, in_review, decided or cancelled."));
            }

            statusFilter = parsed;
        }

        var query = new ProbationReviewSearchQuery(statusFilter, overdue, departmentId, pageRequest);

        return ExecuteAsync(async actor =>
        {
            var result = await service.SearchAsync(query, actor, cancellationToken);
            return Ok(ToPage(result, ProbationReviewResponse.From));
        });
    }

    [HttpPost("{reviewId:long:min(1)}/{transition:regex(^(decide|cancel|unlock)$)}", Name = "decideProbationReview")]
    [Authorize(Policy = ProbationPolicies.Manage)]
    public Task<IActionResult> Transition(
        long reviewId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ProbationDecisionRequest? request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            if (!ProbationReviewActionNames.TryParseContract(transition, out var action))
            {
                return ValidationProblemResult("action", "action must be one of decide, cancel, unlock.");
            }

            var view = await service.TransitionAsync(
                new TransitionProbationReviewCommand(
                    reviewId,
                    action,
                    expectedVersion,
                    request?.ToDecision() ?? ProbationDecision.Empty,
                    actor),
                cancellationToken);

            SetETag(view.Review.Version);
            return Ok(ProbationReviewResponse.From(view));
        });
}
