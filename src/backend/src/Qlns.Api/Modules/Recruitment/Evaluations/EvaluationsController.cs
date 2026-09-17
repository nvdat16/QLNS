using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

namespace Qlns.Api.Modules.Recruitment.Evaluations;

/// <summary>
/// REC-05.1: list scorecards under the blind-evaluation policy, submit an immutable scorecard (Idempotency-Key
/// required; the natural key is interview + evaluator + version) and unlock a scorecard as a new version.
/// </summary>
[Route("api/v1/recruitment")]
public sealed class EvaluationsController(EvaluationService service) : CoreHrControllerBase
{
    public const int IdempotencyKeyMinLength = 16;
    public const int IdempotencyKeyMaxLength = 128;

    [HttpGet("interviews/{interviewId:long:min(1)}/evaluations", Name = "listInterviewEvaluations")]
    [Authorize(Policy = EvaluationPolicies.Read)]
    [ProducesResponseType<EvaluationResponse[]>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(long interviewId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var evaluations = await service.ListAsync(interviewId, actor, cancellationToken);
            return Ok(evaluations.Select(EvaluationResponse.From).ToArray());
        });

    [HttpPost("interviews/{interviewId:long:min(1)}/evaluations", Name = "submitInterviewEvaluation")]
    [Authorize(Policy = EvaluationPolicies.Submit)]
    [ProducesResponseType<EvaluationResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Submit(
        long interviewId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] EvaluationWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsValidIdempotencyKey(idempotencyKey))
        {
            return Task.FromResult<IActionResult>(InvalidIdempotencyKey());
        }

        return ExecuteAsync(async actor =>
        {
            var created = await service.SubmitAsync(new SubmitEvaluationCommand(interviewId, request.ToWrite(), actor), cancellationToken);

            SetETag(created.Version);
            return Created(
                string.Create(CultureInfo.InvariantCulture, $"/api/v1/recruitment/evaluations/{created.Id}"),
                EvaluationResponse.From(created));
        });
    }

    [HttpPost("evaluations/{evaluationId:long:min(1)}/unlock", Name = "unlockInterviewEvaluation")]
    [Authorize(Policy = EvaluationPolicies.Unlock)]
    [ProducesResponseType<EvaluationResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Unlock(
        long evaluationId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] ReasonRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        return ExecuteAsync(async actor =>
        {
            var unlocked = await service.UnlockAsync(
                new UnlockEvaluationCommand(evaluationId, expectedVersion, request.Reason, actor),
                cancellationToken);

            SetETag(unlocked.Version);
            return Ok(EvaluationResponse.From(unlocked));
        });
    }

    private static bool IsValidIdempotencyKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) && key.Length is >= IdempotencyKeyMinLength and <= IdempotencyKeyMaxLength;

    private ObjectResult InvalidIdempotencyKey() => ProblemResult(
        StatusCodes.Status400BadRequest,
        "common.invalid_idempotency_key",
        "Invalid Idempotency-Key header",
        $"Idempotency-Key is required and must be between {IdempotencyKeyMinLength} and {IdempotencyKeyMaxLength} characters.");
}
