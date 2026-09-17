using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.Api.Modules.Recruitment.Offers;

/// <summary>
/// DEVELOPMENT-ONLY endpoint that mints the <c>X-Offer-Token</c> a candidate would receive by e-mail for a <c>sent</c>
/// offer, so the response flow can be exercised locally without the outbox worker. It answers 404 outside the
/// Development environment. In production the token only ever travels inside the offer e-mail.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("dev/offer-token")]
public sealed class DevelopmentOfferTokenController(
    IHostEnvironment environment,
    IOfferRepository repository,
    IOfferResponseTokenService responseTokens,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpGet(Name = "devOfferToken")]
    public async Task<IActionResult> Get([FromQuery] long offerId, CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        if (offerId <= 0)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "offerId must be a positive identifier",
                type: "https://qlns.example/problems/invalid-request");
        }

        var offer = await repository.GetForCandidateAsync(offerId, cancellationToken);
        if (offer is null)
        {
            return NotFound();
        }

        var now = timeProvider.GetUtcNow();
        if (!offer.IsOpenForResponse(DateOnly.FromDateTime(now.UtcDateTime)))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Offer is not open for a candidate response",
                detail: $"Offer {offerId} is {offer.Status.ToContract()}; only sent, unexpired offers receive a token.",
                type: "https://qlns.example/problems/recruitment-offer-not-open");
        }

        var expiresAt = OfferResponseTokenLifetime.ExpiresAt(offer.ExpirationDate);
        return Ok(new
        {
            offerId,
            header = OfferResponsesController.OfferTokenHeader,
            token = responseTokens.Issue(offerId, expiresAt),
            expiresAt
        });
    }
}
