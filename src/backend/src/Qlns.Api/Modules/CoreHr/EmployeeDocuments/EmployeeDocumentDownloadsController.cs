using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

namespace Qlns.Api.Modules.CoreHr.EmployeeDocuments;

/// <summary>EMP-05.1: issue a signed download URL valid for at most 15 minutes after an access check.</summary>
[Route("api/v1/employee-documents")]
public sealed class EmployeeDocumentDownloadsController(EmployeeDocumentService service) : CoreHrControllerBase
{
    [HttpPost("{documentId:long:min(1)}/download-url", Name = "createEmployeeDocumentDownloadUrl")]
    [Authorize(Policy = CoreHrPolicies.DocumentRead)]
    [ProducesResponseType<SignedDownloadResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> CreateDownloadUrl(long documentId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var download = await service.CreateDownloadUrlAsync(documentId, actor, cancellationToken);
            return Ok(new SignedDownloadResponse(download.Url, download.ExpiresAt));
        });
}
