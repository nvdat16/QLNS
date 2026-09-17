using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.Api.Modules.Recruitment.Offers;

/// <summary>
/// REC-06.1: search, read and draft offers and run approve | send | extend | cancel. The route parameter is named
/// <c>transition</c> because attribute routes may not declare a parameter called <c>action</c>; the URL shape is unchanged.
/// </summary>
[Route("api/v1/recruitment/offers")]
public sealed class OffersController(OfferService service) : CoreHrControllerBase
{
    [HttpGet(Name = "listRecruitmentOffers")]
    [Authorize(Policy = OfferPolicies.Read)]
    [ProducesResponseType<PagedResponse<OfferResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] long? applicationId = null,
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return Task.FromResult<IActionResult>(InvalidPage());
        }

        if (applicationId is <= 0)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("applicationId", "applicationId must be a positive identifier."));
        }

        OfferStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!OfferStatusNames.TryParseContract(status, out var parsed))
            {
                return Task.FromResult<IActionResult>(ValidationProblemResult("status", "Unknown offer status. Use draft, approved, sent, accepted, declined, expired or cancelled."));
            }

            statusFilter = parsed;
        }

        var query = new OfferSearchQuery(applicationId, statusFilter, pageRequest);

        return ExecuteAsync(async actor =>
        {
            var result = await service.SearchAsync(query, actor, cancellationToken);
            return Ok(ToPage(result, OfferResponse.From));
        });
    }

    [HttpPost(Name = "createRecruitmentOffer")]
    [Authorize(Policy = OfferPolicies.Write)]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        [FromBody] OfferWriteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var created = await service.CreateAsync(new CreateOfferCommand(request.ToWrite(), actor), cancellationToken);

            SetETag(created.Version);
            return Created(
                string.Create(CultureInfo.InvariantCulture, $"/api/v1/recruitment/offers/{created.Id}"),
                OfferResponse.From(created));
        });

    [HttpGet("{offerId:long:min(1)}", Name = "getRecruitmentOffer")]
    [Authorize(Policy = OfferPolicies.Read)]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get(long offerId, CancellationToken cancellationToken) =>
        ExecuteAsync(async actor =>
        {
            var offer = await service.GetAsync(offerId, actor, cancellationToken);

            SetETag(offer.Version);
            return Ok(OfferResponse.From(offer));
        });

    [HttpPost("{offerId:long:min(1)}/{transition:regex(^(approve|send|extend|cancel)$)}", Name = "transitionRecruitmentOffer")]
    [Authorize(Policy = OfferPolicies.Write)]
    [ProducesResponseType<OfferResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Transition(
        long offerId,
        string transition,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] OfferActionRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        if (!OfferActionNames.TryParseContract(transition, out var action))
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("action", "action must be one of approve, send, extend, cancel."));
        }

        return ExecuteAsync(async actor =>
        {
            var updated = await service.TransitionAsync(
                new TransitionOfferCommand(offerId, action, expectedVersion, request?.Reason, request?.ExpirationDate, actor),
                cancellationToken);

            SetETag(updated.Version);
            return Ok(OfferResponse.From(updated));
        });
    }
}
