using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.Api.Modules.Recruitment.Requisitions;

/// <summary>
/// REC-01.1/REC-01.2: search, create, read and edit requisitions and run their workflow. The transition route
/// parameter is named <c>transition</c> because attribute routes may not declare a parameter called <c>action</c>;
/// the URL shape of the contract is unchanged.
/// </summary>
[Route("api/v1/recruitment/requisitions")]
public sealed class RequisitionsController(RequisitionService service) : CoreHrControllerBase
{
    private const int MaxSearchLength = 200;

    [HttpGet(Name = "listRecruitmentRequisitions")]
    [Authorize(Policy = RequisitionPolicies.Read)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] long? departmentId = null,
        [FromQuery] string? status = null,
        [FromQuery] string? sort = RequisitionSort.CreatedAtDescending,
        CancellationToken cancellationToken = default) => ExecuteAsync(async actor =>
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return InvalidPage();
        }

        if (search is { Length: > MaxSearchLength })
        {
            return ValidationProblemResult("search", $"search must be at most {MaxSearchLength} characters.");
        }

        if (departmentId is < 1)
        {
            return ValidationProblemResult("departmentId", "departmentId must be at least 1.");
        }

        RequisitionStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!RequisitionStatusNames.TryParseContract(status, out var parsedStatus))
            {
                return ValidationProblemResult("status", "status must be one of draft, pending_approval, approved, active_recruiting, closed, cancelled.");
            }

            statusFilter = parsedStatus;
        }

        var normalizedSort = RequisitionSort.Normalize(sort);
        if (!RequisitionSort.IsAllowed(normalizedSort))
        {
            return ValidationProblemResult("sort", $"sort must be one of {string.Join(", ", RequisitionSort.Allowed)}.");
        }

        var query = new RequisitionSearchQuery(
            string.IsNullOrWhiteSpace(search) ? null : search,
            departmentId,
            statusFilter,
            normalizedSort,
            pageRequest);

        var result = await service.SearchAsync(query, actor, cancellationToken);
        return Ok(ToPage(result, RequisitionResponse.From));
    });

    [HttpPost(Name = "createRecruitmentRequisition")]
    [Authorize(Policy = RequisitionPolicies.Write)]
    public Task<IActionResult> Create(
        [FromBody] RequisitionWriteRequest request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        var created = await service.CreateAsync(new CreateRequisitionCommand(request.ToWrite(), actor), cancellationToken);

        SetETag(created.Version);
        return Created(Location(created.Id), RequisitionResponse.From(created));
    });

    [HttpGet("{requisitionId:long:min(1)}", Name = "getRecruitmentRequisition")]
    [Authorize(Policy = RequisitionPolicies.Read)]
    public Task<IActionResult> Get(long requisitionId, CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        var requisition = await service.GetAsync(requisitionId, actor, cancellationToken);

        SetETag(requisition.Version);
        return Ok(RequisitionResponse.From(requisition));
    });

    [HttpPut("{requisitionId:long:min(1)}", Name = "replaceRecruitmentRequisitionDraft")]
    [Authorize(Policy = RequisitionPolicies.Write)]
    public Task<IActionResult> Replace(
        long requisitionId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] RequisitionWriteRequest request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return InvalidIfMatch();
        }

        var updated = await service.ReplaceAsync(
            new ReplaceRequisitionCommand(requisitionId, expectedVersion, request.ToWrite(), actor),
            cancellationToken);

        SetETag(updated.Version);
        return Ok(RequisitionResponse.From(updated));
    });

    [HttpPost("{requisitionId:long:min(1)}/{transition:regex(^(submit|approve|reject|publish|close|cancel)$)}", Name = "transitionRecruitmentRequisition")]
    [Authorize(Policy = RequisitionPolicies.Transition)]
    public Task<IActionResult> Transition(
        long requisitionId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] RequisitionActionRequest? request,
        CancellationToken cancellationToken) => ExecuteAsync(async actor =>
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return InvalidIfMatch();
        }

        if (!RequisitionActionNames.TryParseContract(transition, out var action))
        {
            return ValidationProblemResult("action", "action must be one of submit, approve, reject, publish, close, cancel.");
        }

        var updated = await service.TransitionAsync(
            new TransitionRequisitionCommand(requisitionId, action, expectedVersion, request?.Reason, actor),
            cancellationToken);

        SetETag(updated.Version);
        return Ok(RequisitionResponse.From(updated));
    });

    private static string Location(long requisitionId) =>
        string.Create(CultureInfo.InvariantCulture, $"/api/v1/recruitment/requisitions/{requisitionId}");
}
