using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;

namespace Qlns.Api.Modules.Recruitment.Intake;

/// <summary>REC-02.1/REC-02.2: upload a résumé, follow its intake state and confirm it into an application.</summary>
[Route("api/v1/recruitment")]
public sealed class CandidateIntakeController(CandidateIntakeService service) : CoreHrControllerBase
{
    /// <summary>Kestrel-level ceiling: business limit plus headroom for multipart boundaries and form fields.</summary>
    public const long RequestBodyLimitBytes = 11 * 1024 * 1024;

    private const int IdempotencyKeyMinLength = 16;
    private const int IdempotencyKeyMaxLength = 128;

    [HttpPost("resumes", Name = "startRecruitmentIntake")]
    [Authorize(Policy = IntakePolicies.Write)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestBodyLimitBytes)]
    [ProducesResponseType<CandidateIntakeResponse>(StatusCodes.Status202Accepted)]
    public Task<IActionResult> Upload(
        [FromForm] ResumeUploadRequest request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        var file = request.File;
        if (file is null || file.Length <= 0)
        {
            return ValidationProblemResult("file", "A non-empty file is required.");
        }

        if (file.Length > ResumeUploadRules.MaxSizeBytes)
        {
            return ProblemResult(
                StatusCodes.Status413PayloadTooLarge,
                "recruitment.intake.too_large",
                "File too large",
                $"The résumé exceeds the maximum size of {ResumeUploadRules.MaxSizeBytes} bytes.");
        }

        if (!ResumeUploadRules.IsAllowedContentType(file.ContentType))
        {
            return ProblemResult(
                StatusCodes.Status415UnsupportedMediaType,
                "recruitment.intake.unsupported_media_type",
                "Unsupported file type",
                $"Allowed content types: {string.Join(", ", ResumeUploadRules.AllowedContentTypes)}.");
        }

        await using var stream = file.OpenReadStream();

        var view = await service.StartAsync(
            new StartIntakeCommand(
                request.RequisitionId,
                Path.GetFileName(file.FileName),
                file.ContentType,
                file.Length,
                stream,
                request.PrivacyNoticeVersion,
                request.Consented,
                actor),
            cancellationToken);

        return Accepted(IntakeLocation(view.Intake.IntakeId), CandidateIntakeResponse.From(view));
    });

    [HttpGet("intakes/{intakeId:guid}", Name = "getRecruitmentIntake")]
    [Authorize(Policy = IntakePolicies.Read)]
    public Task<IActionResult> Get(Guid intakeId, CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        var view = await service.GetAsync(intakeId, actor, cancellationToken);
        return Ok(CandidateIntakeResponse.From(view));
    });

    [HttpPost("intakes/{intakeId:guid}/confirm", Name = "confirmRecruitmentIntake")]
    [Authorize(Policy = IntakePolicies.Write)]
    public Task<IActionResult> Confirm(
        Guid intakeId,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] ConfirmIntakeRequest request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        if (idempotencyKey is not { Length: >= IdempotencyKeyMinLength and <= IdempotencyKeyMaxLength })
        {
            return ProblemResult(
                StatusCodes.Status400BadRequest,
                "common.invalid_idempotency_key",
                "Invalid Idempotency-Key header",
                $"Idempotency-Key is required and must be between {IdempotencyKeyMinLength} and {IdempotencyKeyMaxLength} characters.");
        }

        var confirmed = await service.ConfirmAsync(
            new ConfirmIntakeCommand(intakeId, request.Candidate.ToInput(), request.ExistingCandidateId, request.Source, actor),
            cancellationToken);

        SetETag(confirmed.Application.Version);
        return Created(ApplicationLocation(confirmed.Application.Id), RecruitmentApplicationDetailResponse.From(confirmed));
    });

    private static string IntakeLocation(Guid intakeId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/v1/recruitment/intakes/{intakeId}");

    private static string ApplicationLocation(long applicationId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/v1/recruitment/applications/{applicationId}");
}
