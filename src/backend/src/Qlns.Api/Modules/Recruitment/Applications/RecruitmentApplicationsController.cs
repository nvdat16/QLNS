using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.Api.Modules.Recruitment.Applications;

/// <summary>REC-03.2: read one application, advance it exactly one stage, or end it with reject/withdraw.</summary>
[Route("api/v1/recruitment/applications")]
public sealed class RecruitmentApplicationsController(RecruitmentPipelineService service) : CoreHrControllerBase
{
    [HttpGet("{applicationId:long:min(1)}", Name = "getRecruitmentApplication")]
    [Authorize(Policy = ApplicationPolicies.Read)]
    public Task<IActionResult> Get(long applicationId, CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        var application = await service.GetAsync(applicationId, actor, cancellationToken);

        SetETag(application.Version);
        return Ok(RecruitmentApplicationResponse.From(application));
    });

    [HttpPost("{applicationId:long:min(1)}/advance", Name = "advanceRecruitmentApplication")]
    [Authorize(Policy = ApplicationPolicies.Advance)]
    public Task<IActionResult> Advance(
        long applicationId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] AdvanceApplicationRequest request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return InvalidIfMatch();
        }

        if (!ApplicationStageNames.TryParseContract(request.TargetStage, out var targetStage) || !targetStage.IsActive())
        {
            return ValidationProblemResult("targetStage", "targetStage must be one of sourced_applied, ai_screening, tech_interview, executive_round, offer_letter, hired_ready.");
        }

        var application = await service.AdvanceAsync(
            new AdvanceApplicationCommand(applicationId, targetStage, expectedVersion, request.Reason, actor),
            cancellationToken);

        SetETag(application.Version);
        return Ok(RecruitmentApplicationResponse.From(application));
    });

    [HttpPost("{applicationId:long:min(1)}/{terminalAction:regex(^(reject|withdraw)$)}", Name = "terminateRecruitmentApplication")]
    [Authorize(Policy = ApplicationPolicies.Terminate)]
    public Task<IActionResult> Terminate(
        long applicationId,
        string terminalAction,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] ReasonRequest request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return InvalidIfMatch();
        }

        if (!ApplicationTerminalActionNames.TryParseContract(terminalAction, out var action))
        {
            return ValidationProblemResult("terminalAction", "terminalAction must be reject or withdraw.");
        }

        var application = await service.TerminateAsync(
            new TerminateApplicationCommand(applicationId, action, expectedVersion, request.Reason, actor),
            cancellationToken);

        SetETag(application.Version);
        return Ok(RecruitmentApplicationResponse.From(application));
    });
}
