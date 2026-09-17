using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;

namespace Qlns.Api.Modules.CoreHr.Probation;

/// <summary>
/// EMP-06.1: the probation review of one employee's current probation contract — read it and submit the assessment.
/// The endpoint policy requires <c>corehr.probation.read</c>; the service enforces that only the assigned reviewer
/// (or <c>corehr.probation.manage</c>) may submit.
/// </summary>
[Route("api/v1/employees/{employeeId:long:min(1)}/probation-review")]
public sealed class EmployeeProbationReviewController(ProbationReviewService service) : CoreHrControllerBase
{
    [HttpGet(Name = "getProbationReview")]
    [Authorize(Policy = ProbationPolicies.Read)]
    public Task<IActionResult> Get(long employeeId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var view = await service.GetCurrentAsync(employeeId, actor, cancellationToken);
            SetETag(view.Review.Version);
            return Ok(ProbationReviewResponse.From(view));
        });

    [HttpPut(Name = "submitProbationReview")]
    [Authorize(Policy = ProbationPolicies.Read)]
    public Task<IActionResult> Submit(
        long employeeId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] ProbationReviewWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            var view = await service.SubmitAsync(
                new SubmitProbationReviewCommand(employeeId, expectedVersion, request.ToWrite(), actor),
                cancellationToken);

            SetETag(view.Review.Version);
            return Ok(ProbationReviewResponse.From(view));
        });
}
