using System.Globalization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.Api.Modules.Recruitment.Applications;

[ApiController]
[Route("api/v1/recruitment/applications")]
public sealed class RecruitmentApplicationsController(RecruitmentPipelineService service) : ControllerBase
{
    [HttpGet("{applicationId:long:min(1)}", Name = "getRecruitmentApplication")]
    [Authorize(Policy = "RecruitmentRead")]
    public async Task<IActionResult> Get(
        long applicationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var application = await service.GetAsync(applicationId, GetDataScope(), cancellationToken);
            Response.Headers.ETag = QuoteVersion(application.Version);
            return Ok(ToResponse(application));
        }
        catch (ApplicationNotFoundException exception)
        {
            return ProblemResult(404, "common.not_found", "Application not found", exception.Message);
        }
    }

    [HttpPost("{applicationId:long:min(1)}/advance", Name = "advanceRecruitmentApplication")]
    [Authorize(Policy = "RecruitmentAdvance")]
    public async Task<IActionResult> Advance(
        long applicationId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] AdvanceApplicationRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return ProblemResult(400, "common.invalid_etag", "Invalid If-Match header", "Use a quoted positive numeric ETag, for example \"4\".");
        }

        if (!ApplicationStageNames.TryParseContract(request.TargetStage, out var targetStage))
        {
            var validation = new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["targetStage"] = ["Unknown recruitment stage."]
            })
            {
                Status = 422,
                Title = "Request validation failed",
                Type = "https://qlns.example/problems/validation",
                Instance = HttpContext.Request.Path
            };
            validation.Extensions["correlationId"] = HttpContext.TraceIdentifier;
            return UnprocessableEntity(validation);
        }

        if (!long.TryParse(User.FindFirstValue("qlns_user_id"), NumberStyles.None, CultureInfo.InvariantCulture, out var actorUserId))
        {
            return ProblemResult(403, "common.actor_not_mapped", "Actor is not mapped", "The authenticated identity has no internal QLNS user mapping.");
        }

        var correlationId = HttpContext.TraceIdentifier;

        try
        {
            var application = await service.AdvanceAsync(
                new AdvanceApplicationCommand(
                    applicationId,
                    targetStage,
                    expectedVersion,
                    actorUserId,
                    GetDataScope(),
                    correlationId,
                    request.Reason),
                cancellationToken);

            Response.Headers.ETag = QuoteVersion(application.Version);
            return Ok(ToResponse(application));
        }
        catch (ApplicationNotFoundException exception)
        {
            return ProblemResult(404, "common.not_found", "Application not found", exception.Message);
        }
        catch (ConcurrencyConflictException exception)
        {
            return ProblemResult(409, "common.concurrency_conflict", "The application changed concurrently", exception.Message);
        }
        catch (BusinessRuleException exception)
        {
            return ProblemResult(409, exception.Code, "Invalid recruitment stage transition", exception.Message);
        }
    }

    private ObjectResult ProblemResult(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://qlns.example/problems/{code.Replace('.', '-')}",
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }

    private static RecruitmentApplicationResponse ToResponse(RecruitmentApplication application) => new(
        application.Id,
        application.CandidateId,
        application.JobPostingId,
        application.Stage.ToContract(),
        application.Version,
        application.UpdatedAt);

    private static bool TryParseVersion(string? value, out long version)
    {
        version = 0;
        return value is { Length: >= 3 } &&
            value[0] == '"' && value[^1] == '"' &&
            long.TryParse(value[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out version) &&
            version > 0;
    }

    private static string QuoteVersion(long version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    private RecruitmentDataScope GetDataScope()
    {
        var organizationWide = User.HasClaim("data_scope", "organization");
        var departmentIds = User.FindAll("department_id")
            .Select(claim => long.TryParse(claim.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        return new RecruitmentDataScope(organizationWide, departmentIds);
    }
}

public sealed record AdvanceApplicationRequest(
    [property: Required, MaxLength(40)] string TargetStage,
    [property: MaxLength(1000)] string? Reason);

public sealed record RecruitmentApplicationResponse(
    long Id,
    long CandidateId,
    long JobPostingId,
    string Stage,
    long Version,
    DateTimeOffset UpdatedAt);
