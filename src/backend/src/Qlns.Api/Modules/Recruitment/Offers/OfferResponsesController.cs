using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.Api.Modules.Recruitment.Offers;

/// <summary>
/// REC-06.2: the candidate accepts or declines through a link e-mailed after <c>send</c>. There is no bearer identity:
/// the request is authenticated by <c>X-Offer-Token</c> (OpenAPI security scheme offerToken), validated before anything
/// about the offer is revealed. Acceptance runs the idempotent employee handoff (sequence diagram §4).
/// Exceptions are mapped here rather than through <see cref="CoreHrControllerBase.ExecuteAsync"/> because that helper
/// resolves a workforce actor from the JWT, which this anonymous endpoint does not have.
/// </summary>
[AllowAnonymous]
[Route("api/v1/recruitment/offers")]
public sealed class OfferResponsesController(
    OfferService service,
    IOfferResponseTokenService responseTokens,
    TimeProvider timeProvider) : CoreHrControllerBase
{
    public const string InvalidTokenCode = "recruitment.offer.invalid_token";
    public const string OfferTokenHeader = "X-Offer-Token";
    public const int IdempotencyKeyMinLength = 16;
    public const int IdempotencyKeyMaxLength = 128;

    [HttpPost("{offerId:long:min(1)}/response", Name = "respondToRecruitmentOffer")]
    [ProducesResponseType<OfferDecisionResultResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Respond(
        long offerId,
        [FromHeader(Name = OfferTokenHeader)] string? offerToken,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] OfferDecisionRequest request,
        CancellationToken cancellationToken)
    {
        if (!responseTokens.TryValidate(offerToken, offerId, timeProvider.GetUtcNow()))
        {
            return ProblemResult(
                StatusCodes.Status401Unauthorized,
                InvalidTokenCode,
                "Offer token is missing, invalid or expired",
                "Provide the X-Offer-Token header from the offer e-mail for this offer.");
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey) ||
            idempotencyKey.Length is < IdempotencyKeyMinLength or > IdempotencyKeyMaxLength)
        {
            return ProblemResult(
                StatusCodes.Status400BadRequest,
                "common.invalid_idempotency_key",
                "Invalid Idempotency-Key header",
                $"Idempotency-Key is required and must be between {IdempotencyKeyMinLength} and {IdempotencyKeyMaxLength} characters.");
        }

        if (!OfferDecisionNames.TryParseContract(request.Decision, out var decision))
        {
            return ValidationProblemResult("decision", "decision must be accept or decline.");
        }

        try
        {
            var result = await service.RespondAsync(
                new RespondToOfferCommand(offerId, decision, request.Reason, CorrelationId),
                cancellationToken);

            return Ok(OfferDecisionResultResponse.From(result));
        }
        catch (CoreHrNotFoundException exception)
        {
            return ProblemResult(StatusCodes.Status404NotFound, "common.not_found", $"{exception.Resource} not found", exception.Message);
        }
        catch (CoreHrConcurrencyConflictException exception)
        {
            return ProblemResult(StatusCodes.Status409Conflict, "common.concurrency_conflict", "Resource changed concurrently", exception.Message);
        }
        catch (CoreHrBusinessRuleException exception)
        {
            var result = ProblemResult(StatusCodes.Status409Conflict, exception.Code, "Business rule violated", exception.Message);
            if (result.Value is ProblemDetails problem)
            {
                foreach (var (key, value) in exception.Details)
                {
                    problem.Extensions[key] = value;
                }
            }

            return result;
        }
        catch (CoreHrValidationException exception)
        {
            return ValidationProblemResult(exception.Errors);
        }
    }
}
