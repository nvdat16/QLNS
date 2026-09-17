using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;

namespace Qlns.Api.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// DEVELOPMENT-ONLY endpoint that serves the file behind a signed URL produced by
/// <see cref="FileSystemDocumentStorage"/>. It answers 404 outside the Development environment.
/// In production the object store itself serves pre-signed URLs and this controller is never hit.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("dev/document-content")]
public sealed class DevelopmentDocumentContentController(
    IHostEnvironment environment,
    DocumentStorageOptions options,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet(Name = "devDocumentContent")]
    public IActionResult Get(
        [FromQuery] string? key,
        [FromQuery] string? name,
        [FromQuery] long expires,
        [FromQuery] string? sig)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (!FileSystemDocumentStorage.TryValidateSignature(options, key, name, expires, sig, timeProvider.GetUtcNow()))
        {
            return Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Download link is invalid or expired",
                type: "https://qlns.example/problems/corehr-document-link-invalid");
        }

        if (!FileSystemDocumentStorage.TryResolvePhysicalPath(options, key, out var path) || !System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var downloadName = Path.GetFileName(name!);
        return PhysicalFile(path, "application/octet-stream", string.IsNullOrEmpty(downloadName) ? "document" : downloadName);
    }
}
