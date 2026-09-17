using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Contracts.Shared;

namespace Qlns.Api.Modules.Contracts.Shared;

/// <summary>
/// Adds the multipart signed-document guard shared by the contract and addendum upload endpoints to the
/// application-wide <see cref="CoreHrControllerBase"/>: empty file ⇒ 422, over 10 MiB ⇒ 413, not a PDF ⇒ 415.
/// </summary>
public abstract class ContractsControllerBase : CoreHrControllerBase
{
    /// <summary>Kestrel-level ceiling: business limit plus headroom for multipart boundaries.</summary>
    public const long RequestBodyLimitBytes = 11 * 1024 * 1024;

    /// <summary>Returns the Problem Details result to answer with, or null when the file passes the early checks.</summary>
    protected IActionResult? RejectInvalidSignedDocument(IFormFile? file, string codePrefix)
    {
        if (file is null || file.Length <= 0)
        {
            return ValidationProblemResult("file", "A non-empty PDF file is required.");
        }

        if (file.Length > SignedDocumentRules.MaxSizeBytes)
        {
            return ProblemResult(
                StatusCodes.Status413PayloadTooLarge,
                $"{codePrefix}.too_large",
                "File too large",
                $"The file exceeds the maximum size of {SignedDocumentRules.MaxSizeBytes} bytes.");
        }

        if (!SignedDocumentRules.IsPdf(file.ContentType))
        {
            return ProblemResult(
                StatusCodes.Status415UnsupportedMediaType,
                $"{codePrefix}.unsupported_media_type",
                "Unsupported file type",
                $"Signed documents must be {SignedDocumentRules.PdfContentType}.");
        }

        return null;
    }

    protected static SignedDocumentUpload ToUpload(IFormFile file, Stream content) => new(
        Path.GetFileName(file.FileName),
        file.ContentType,
        file.Length,
        content);
}

/// <summary>Multipart body of the signed-document endpoints: a single <c>file</c> part.</summary>
public sealed record SignedDocumentUploadRequest(IFormFile? File);
