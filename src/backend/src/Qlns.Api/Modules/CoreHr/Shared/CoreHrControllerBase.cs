using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Shared;

/// <summary>
/// Shared HTTP plumbing for Core HR controllers: actor resolution, ETag/If-Match handling and the
/// mapping from business exceptions to RFC 9457 Problem Details with a stable <c>code</c> extension.
/// </summary>
[ApiController]
public abstract class CoreHrControllerBase : ControllerBase
{
    protected string CorrelationId => HttpContext.TraceIdentifier;

    /// <summary>Resolves the actor and runs the action, translating Core HR exceptions to Problem Details.</summary>
    protected async Task<IActionResult> ExecuteAsync(Func<CoreHrActor, Task<IActionResult>> action)
    {
        if (!CoreHrActorResolver.TryResolve(User, CorrelationId, out var actor))
        {
            return ProblemResult(403, "common.actor_not_mapped", "Actor is not mapped",
                "The authenticated identity has no internal QLNS user mapping.");
        }

        try
        {
            return await action(actor);
        }
        catch (CoreHrNotFoundException exception)
        {
            return ProblemResult(404, "common.not_found", $"{exception.Resource} not found", exception.Message);
        }
        catch (CoreHrForbiddenException exception)
        {
            return ProblemResult(403, exception.Code, "Access forbidden", exception.Message);
        }
        catch (CoreHrConcurrencyConflictException exception)
        {
            return ProblemResult(409, "common.concurrency_conflict", "Resource changed concurrently", exception.Message);
        }
        catch (CoreHrBusinessRuleException exception)
        {
            var result = ProblemResult(409, exception.Code, "Business rule violated", exception.Message);
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

    protected ObjectResult ProblemResult(int status, string code, string title, string detail)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Type = $"https://qlns.example/problems/{code.Replace('.', '-')}",
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = CorrelationId;
        return StatusCode(status, problem);
    }

    protected ObjectResult ValidationProblemResult(IReadOnlyDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors.ToDictionary(pair => pair.Key, pair => pair.Value))
        {
            Status = 422,
            Title = "Request validation failed",
            Type = "https://qlns.example/problems/validation",
            Instance = HttpContext.Request.Path
        };
        problem.Extensions["code"] = "common.validation_failed";
        problem.Extensions["correlationId"] = CorrelationId;
        return UnprocessableEntity(problem);
    }

    protected ObjectResult ValidationProblemResult(string field, string message) =>
        ValidationProblemResult(new Dictionary<string, string[]> { [field] = [message] });

    protected ObjectResult InvalidIfMatch() => ProblemResult(400, "common.invalid_etag",
        "Invalid If-Match header", "Use a quoted positive numeric ETag, for example \"4\".");

    protected static bool TryParseVersion(string? value, out long version)
    {
        version = 0;
        return value is { Length: >= 3 } &&
            value[0] == '"' && value[^1] == '"' &&
            long.TryParse(value[1..^1], NumberStyles.None, CultureInfo.InvariantCulture, out version) &&
            version > 0;
    }

    protected static string QuoteVersion(long version) => $"\"{version.ToString(CultureInfo.InvariantCulture)}\"";

    protected void SetETag(long version) => Response.Headers.ETag = QuoteVersion(version);

    protected static bool TryCreatePage(int page, int pageSize, out PageRequest request)
    {
        request = null!;
        if (page < 1 || pageSize < 1 || pageSize > PageRequest.MaxPageSize)
        {
            return false;
        }

        request = new PageRequest(page, pageSize);
        return true;
    }

    protected ObjectResult InvalidPage() => ValidationProblemResult(new Dictionary<string, string[]>
    {
        ["page"] = ["page must be at least 1."],
        ["pageSize"] = [$"pageSize must be between 1 and {PageRequest.MaxPageSize}."]
    });

    protected static PagedResponse<TOut> ToPage<TIn, TOut>(PagedResult<TIn> result, Func<TIn, TOut> map) => new(
        result.Items.Select(map).ToList(),
        new PageMetadataResponse(result.Page, result.PageSize, result.TotalItems, result.TotalPages));
}

public sealed record PageMetadataResponse(int Page, int PageSize, long TotalItems, int TotalPages);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, PageMetadataResponse Page);
