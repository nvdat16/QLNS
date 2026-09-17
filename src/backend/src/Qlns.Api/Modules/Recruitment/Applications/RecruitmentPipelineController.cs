using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.Api.Modules.Recruitment.Applications;

/// <summary>REC-03.1: the filtered, per-column paged Kanban pipeline of one requisition.</summary>
[Route("api/v1/recruitment/pipeline")]
public sealed class RecruitmentPipelineController(RecruitmentPipelineService service) : CoreHrControllerBase
{
    private const int MaxSearchLength = 200;

    [HttpGet(Name = "getRecruitmentPipeline")]
    [Authorize(Policy = ApplicationPolicies.Read)]
    public Task<IActionResult> Get(
        [FromQuery] long? requisitionId = null,
        [FromQuery] string? search = null,
        [FromQuery] string? stage = null,
        [FromQuery] decimal? minimumAiScore = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) => ExecuteAsync(async actor =>
    {
        if (requisitionId is not >= 1)
        {
            return ValidationProblemResult("requisitionId", "requisitionId is required and must be at least 1.");
        }

        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return InvalidPage();
        }

        if (search is { Length: > MaxSearchLength })
        {
            return ValidationProblemResult("search", $"search must be at most {MaxSearchLength} characters.");
        }

        ApplicationStage? stageFilter = null;
        if (!string.IsNullOrWhiteSpace(stage))
        {
            if (!ApplicationStageNames.TryParseContract(stage, out var parsedStage))
            {
                return ValidationProblemResult("stage", "stage must be one of sourced_applied, ai_screening, tech_interview, executive_round, offer_letter, hired_ready, rejected, withdrawn.");
            }

            stageFilter = parsedStage;
        }

        if (minimumAiScore is < 0 or > 100)
        {
            return ValidationProblemResult("minimumAiScore", "minimumAiScore must be between 0 and 100.");
        }

        var pipeline = await service.GetPipelineAsync(
            new RecruitmentPipelineQuery(
                requisitionId.Value,
                string.IsNullOrWhiteSpace(search) ? null : search,
                stageFilter,
                minimumAiScore,
                pageRequest),
            actor,
            cancellationToken);

        return Ok(RecruitmentPipelineResponse.From(pipeline));
    });
}
