using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.Contracts.Shared;
using Qlns.Api.Modules.CoreHr.EmployeeDocuments;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.Contracts.Contracts;

/// <summary>
/// CON-01 / CON-02 endpoints: search, draft, read, edit, workflow transitions, signed-document upload, signed
/// download links and the expiry dashboard. The literal <c>expiring</c> segment is matched before the
/// <c>{contractId}</c> template, and the transition route parameter is named <c>transition</c> because attribute
/// routes may not declare a parameter called <c>action</c>; the URL shapes follow the OpenAPI document.
/// </summary>
[Route("api/v1/contracts")]
public sealed class ContractsController(ContractService service) : ContractsControllerBase
{
    private const string ProblemCodePrefix = "contracts.contract";

    [HttpGet(Name = "listContracts")]
    [Authorize(Policy = ContractPolicies.Read)]
    [ProducesResponseType<PagedResponse<ContractResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] long? employeeId = null,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async actor =>
        {
            if (!TryCreatePage(page, pageSize, out var pageRequest))
            {
                return InvalidPage();
            }

            if (employeeId is <= 0)
            {
                return ValidationProblemResult("employeeId", "employeeId must be a positive identifier.");
            }

            ContractType? typeFilter = null;
            if (!string.IsNullOrWhiteSpace(type))
            {
                if (!ContractTypeNames.TryParseContract(type, out var parsedType))
                {
                    return ValidationProblemResult("type", "Unknown contract type. Use probation, fixed_term, indefinite, internship or service_contract.");
                }

                typeFilter = parsedType;
            }

            ContractStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!ContractStatusNames.TryParseContract(status, out var parsedStatus))
                {
                    return ValidationProblemResult("status", "Unknown contract status. Use draft, approved, executed, active, expired, terminated or cancelled.");
                }

                statusFilter = parsedStatus;
            }

            var result = await service.SearchAsync(
                new ContractSearchQuery(employeeId, typeFilter, statusFilter, pageRequest),
                actor,
                cancellationToken);

            return Ok(ToPage(result, ContractResponse.From));
        });

    [HttpPost(Name = "createContract")]
    [Authorize(Policy = ContractPolicies.Write)]
    [ProducesResponseType<ContractResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        [FromBody] ContractWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.CreateAsync(new CreateContractCommand(request.ToWrite(), actor), cancellationToken);

            SetETag(created.Version);
            return Created(Location(created.Id), ContractResponse.From(created));
        });

    [HttpGet("expiring", Name = "listExpiringContracts")]
    [Authorize(Policy = ContractPolicies.Read)]
    [ProducesResponseType<ExpiringContractPageResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> ListExpiring(
        [FromQuery] DateOnly? asOf = null,
        [FromQuery] int withinDays = ExpiryAlertPolicy.DefaultWithinDays,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async actor =>
        {
            if (!TryCreatePage(page, pageSize, out var pageRequest))
            {
                return InvalidPage();
            }

            var view = await service.ListExpiringAsync(
                new ExpiringContractsQuery(asOf, withinDays, pageRequest),
                actor,
                cancellationToken);

            var pageResponse = ToPage(view.Contracts, ExpiringContractResponse.From);
            return Ok(new ExpiringContractPageResponse(pageResponse.Items, pageResponse.Page, view.AsOf));
        });

    [HttpGet("{contractId:long:min(1)}", Name = "getContract")]
    [Authorize(Policy = ContractPolicies.Read)]
    [ProducesResponseType<ContractResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get(long contractId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var contract = await service.GetAsync(contractId, actor, cancellationToken);

            SetETag(contract.Version);
            return Ok(ContractResponse.From(contract));
        });

    [HttpPut("{contractId:long:min(1)}", Name = "replaceContractDraft")]
    [Authorize(Policy = ContractPolicies.Write)]
    [ProducesResponseType<ContractResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Replace(
        long contractId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] ContractWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            var updated = await service.ReplaceAsync(
                new ReplaceContractCommand(contractId, expectedVersion, request.ToWrite(), actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(ContractResponse.From(updated));
        });

    [HttpPost("{contractId:long:min(1)}/{transition:regex(^(approve|activate|terminate|cancel)$)}", Name = "transitionContract")]
    [Authorize(Policy = ContractPolicies.Write)]
    [ProducesResponseType<ContractResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Transition(
        long contractId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ContractActionRequest? request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            if (!TryParseVersion(ifMatch, out var expectedVersion))
            {
                return InvalidIfMatch();
            }

            if (!ContractActionNames.TryParseContract(transition, out var action))
            {
                return ValidationProblemResult("action", "action must be one of approve, activate, terminate, cancel.");
            }

            var updated = await service.TransitionAsync(
                new TransitionContractCommand(
                    contractId,
                    action,
                    expectedVersion,
                    request?.ToOptions() ?? ContractActionOptions.None,
                    actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(ContractResponse.From(updated));
        });

    [HttpPost("{contractId:long:min(1)}/signed-document", Name = "uploadSignedContractDocument")]
    [Authorize(Policy = ContractPolicies.Write)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RequestBodyLimitBytes)]
    [ProducesResponseType<ContractResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> UploadSignedDocument(
        long contractId,
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
                new UploadSignedContractCommand(contractId, expectedVersion, ToUpload(file, stream), actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(ContractResponse.From(updated));
        });

    [HttpPost("{contractId:long:min(1)}/download-url", Name = "createContractDownloadUrl")]
    [Authorize(Policy = ContractPolicies.Read)]
    [ProducesResponseType<SignedDownloadResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> CreateDownloadUrl(long contractId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var download = await service.CreateDownloadUrlAsync(contractId, actor, cancellationToken);
            return Ok(new SignedDownloadResponse(download.Url, download.ExpiresAt));
        });

    private static string Location(long contractId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/v1/contracts/{contractId}");
}
