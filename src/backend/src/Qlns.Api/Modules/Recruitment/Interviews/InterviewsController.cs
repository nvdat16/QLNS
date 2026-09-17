using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;

namespace Qlns.Api.Modules.Recruitment.Interviews;

/// <summary>
/// REC-04.1: search the interview calendar, schedule an interview (double-booking detection, .ics invitation via
/// outbox) and run reschedule | complete | cancel. The route parameter is named <c>transition</c> because attribute
/// routes may not declare a parameter called <c>action</c>; the URL shape is unchanged.
/// </summary>
[Route("api/v1/recruitment/interviews")]
public sealed class InterviewsController(InterviewService service) : CoreHrControllerBase
{
    [HttpGet(Name = "listRecruitmentInterviews")]
    [Authorize(Policy = InterviewPolicies.Read)]
    [ProducesResponseType<PagedResponse<InterviewResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] long? applicationId = null,
        [FromQuery] long? interviewerUserId = null,
        [FromQuery(Name = "from")] DateTimeOffset? from = null,
        [FromQuery(Name = "to")] DateTimeOffset? to = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return Task.FromResult<IActionResult>(InvalidPage());
        }

        if (applicationId is <= 0)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("applicationId", "applicationId must be a positive identifier."));
        }

        if (interviewerUserId is <= 0)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("interviewerUserId", "interviewerUserId must be a positive identifier."));
        }

        if (from is { } windowStart && to is { } windowEnd && windowEnd < windowStart)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("to", "to must not be before from."));
        }

        InterviewStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!InterviewStatusNames.TryParseContract(status, out var parsed))
            {
                return Task.FromResult<IActionResult>(ValidationProblemResult("status", "Unknown interview status. Use scheduled, completed, cancelled or no_show."));
            }

            statusFilter = parsed;
        }

        var query = new InterviewSearchQuery(applicationId, interviewerUserId, from, to, statusFilter, pageRequest);

        return ExecuteAsync(async actor =>
        {
            var result = await service.SearchAsync(query, actor, cancellationToken);
            return Ok(ToPage(result, InterviewResponse.From));
        });
    }

    [HttpPost(Name = "scheduleRecruitmentInterview")]
    [Authorize(Policy = InterviewPolicies.Manage)]
    [ProducesResponseType<InterviewResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Schedule(
        [FromBody] InterviewWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.ScheduleAsync(new ScheduleInterviewCommand(request.ToWrite(), actor), cancellationToken);

            SetETag(created.Version);
            return Created(
                string.Create(CultureInfo.InvariantCulture, $"/api/v1/recruitment/interviews/{created.Id}"),
                InterviewResponse.From(created));
        });

    [HttpPost("{interviewId:long:min(1)}/{transition:regex(^(reschedule|complete|cancel)$)}", Name = "transitionRecruitmentInterview")]
    [Authorize(Policy = InterviewPolicies.Read)]
    [ProducesResponseType<InterviewResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Transition(
        long interviewId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] InterviewActionRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        if (!InterviewActionNames.TryParseContract(transition, out var action))
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("action", "action must be one of reschedule, complete, cancel."));
        }

        return ExecuteAsync(async actor =>
        {
            var updated = await service.TransitionAsync(
                new TransitionInterviewCommand(interviewId, action, expectedVersion, request?.ToChange(), actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(InterviewResponse.From(updated));
        });
    }
}
