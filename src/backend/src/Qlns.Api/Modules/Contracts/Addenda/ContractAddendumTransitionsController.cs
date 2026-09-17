using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;

namespace Qlns.Api.Modules.Contracts.Addenda;

/// <summary>
/// CON-03.1 workflow on one addendum: <c>POST /contract-addenda/{addendumId}/{action}</c> with action
/// submit | approve | mark-signed | make-effective | cancel, and the signed-document upload. The route parameter is
/// named <c>transition</c> because attribute routes may not declare a parameter called <c>action</c>.
/// </summary>
[Route("api/v1/contract-addenda")]
public sealed class ContractAddendumTransitionsController(ContractAddendumService service) : ContractsControllerBase
{
    private const string ProblemCodePrefix = "contracts.addendum";

    [HttpPost("{addendumId:long:min(1)}/{transition:regex(^(submit|approve|mark-signed|make-effective|cancel)$)}", Name = "transitionContractAddendum")]
    [Authorize(Policy = ContractPolicies.Write)]
    [ProducesResponseType<ContractAddendumResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Transition(
        long addendumId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReasonRequest? request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            if (!ContractAddendumActionNames.TryParseContract(transition, out var action))
            {
                return ValidationProblemResult("action", "action must be one of submit, approve, mark-signed, make-effective, cancel.");
            }

            var updated = await service.TransitionAsync(
                new TransitionContractAddendumCommand(addendumId, action, expectedVersion, request?.Reason, actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(ContractAddendumResponse.From(updated));
        });

    [HttpPost("{addendumId:long:min(1)}/signed-document", Name = "uploadSignedContractAddendum")]
    [Authorize(Policy = ContractPolicies.Write)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestBodyLimitBytes)]
    [ProducesResponseType<ContractAddendumResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> UploadSignedDocument(
        long addendumId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromForm] SignedDocumentUploadRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            if (RejectInvalidSignedDocument(request.File, ProblemCodePrefix) is { } rejected)
            {
                return rejected;
            }

            var file = request.File!;
            await using var stream = file.OpenReadStream();

            var updated = await service.AttachSignedDocumentAsync(
                new UploadSignedAddendumCommand(addendumId, expectedVersion, ToUpload(file, stream), actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(ContractAddendumResponse.From(updated));
        });
}
