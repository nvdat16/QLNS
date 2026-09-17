using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

namespace Qlns.Api.Modules.CoreHr.EmployeeDocuments;

/// <summary>EMP-05.1: list authorized document metadata and upload new document versions.</summary>
[Route("api/v1/employees/{employeeId:long:min(1)}/documents")]
public sealed class EmployeeDocumentsController(EmployeeDocumentService service) : CoreHrControllerBase
{
    /// <summary>Kestrel-level ceiling: business limit plus headroom for multipart boundaries and form fields.</summary>
    public const long RequestBodyLimitBytes = 11 * 1024 * 1024;

    [HttpGet(Name = "listEmployeeDocuments")]
    [Authorize(Policy = CoreHrPolicies.DocumentRead)]
    [ProducesResponseType<EmployeeDocumentResponse[]>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(long employeeId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var documents = await service.ListAsync(employeeId, actor, cancellationToken);
            return Ok(documents.Select(EmployeeDocumentResponse.From).ToArray());
        });

    [HttpPost(Name = "uploadEmployeeDocument")]
    [Authorize(Policy = CoreHrPolicies.DocumentUpload)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestBodyLimitBytes)]
    [ProducesResponseType<EmployeeDocumentResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Upload(
        long employeeId,
        [FromForm] EmployeeDocumentUploadRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var file = request.File;
            if (file is null || file.Length <= 0)
            {
                return ValidationProblemResult("file", "A non-empty file is required.");
            }

            if (file.Length > DocumentUploadRules.MaxSizeBytes)
            {
                return ProblemResult(
                    StatusCodes.Status413PayloadTooLarge,
                    "corehr.document.too_large",
                    "File too large",
                    $"The file exceeds the maximum size of {DocumentUploadRules.MaxSizeBytes} bytes.");
            }

            if (!DocumentUploadRules.IsAllowedContentType(file.ContentType))
            {
                return ProblemResult(
                    StatusCodes.Status415UnsupportedMediaType,
                    "corehr.document.unsupported_media_type",
                    "Unsupported file type",
                    $"Allowed content types: {string.Join(", ", DocumentUploadRules.AllowedContentTypes)}.");
            }

            await using var stream = file.OpenReadStream();

            var document = await service.UploadAsync(
                new UploadEmployeeDocumentCommand(
                    employeeId,
                    request.DocumentType,
                    Path.GetFileName(file.FileName),
                    file.ContentType,
                    file.Length,
                    stream,
                    request.RetentionUntil,
                    actor),
                cancellationToken);

            var location = string.Create(CultureInfo.InvariantCulture, $"/api/v1/employee-documents/{document.Id}");
            return Created(location, EmployeeDocumentResponse.From(document));
        });
}
