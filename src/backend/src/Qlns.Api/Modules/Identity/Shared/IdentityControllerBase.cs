using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.Api.Modules.Identity.Shared;

/// <summary>
/// Shared plumbing of the ADM controllers. It adds two things to <see cref="CoreHrControllerBase"/>:
/// the mapping of <see cref="AuthenticationFailedException"/> to a 401 Problem Details, and an executor for
/// the anonymous endpoints (sign-in, refresh, sign-out) which have no actor to resolve yet.
/// </summary>
[ApiController]
public abstract class IdentityControllerBase : CoreHrControllerBase
{
    /// <summary>Client fingerprint recorded on the refresh token and in the audit trail.</summary>
    protected SignInContext SignInContext => new(
        HttpContext.Connection.RemoteIpAddress?.ToString(),
        Request.Headers.UserAgent.ToString() is { Length: > 0 } agent ? agent : null,
        CorrelationId);

    /// <summary>Runs an anonymous action, translating module exceptions to Problem Details.</summary>
    protected async Task<IActionResult> ExecuteAnonymousAsync(Func<Task<IActionResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            return await action();
        }
        catch (AuthenticationFailedException exception)
        {
            return AuthenticationProblem(exception);
        }
        catch (CoreHrNotFoundException exception)
        {
            return ProblemResult(StatusCodes.Status404NotFound, "common.not_found", $"{exception.Resource} not found", exception.Message);
        }
        catch (CoreHrValidationException exception)
        {
            return ValidationProblemResult(exception.Errors);
        }
    }

    /// <summary>Resolves the actor as usual and additionally maps authentication failures to 401.</summary>
    protected Task<IActionResult> ExecuteIdentityAsync(Func<CoreHrActor, Task<IActionResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return ExecuteAsync(async actor =>
        {
            try
            {
                return await action(actor);
            }
            catch (AuthenticationFailedException exception)
            {
                return AuthenticationProblem(exception);
            }
        });
    }

    private ObjectResult AuthenticationProblem(AuthenticationFailedException exception)
    {
        var result = ProblemResult(
            StatusCodes.Status401Unauthorized,
            exception.Code,
            "Authentication failed",
            exception.Message);

        if (result.Value is ProblemDetails problem)
        {
            foreach (var (key, value) in exception.Details)
            {
                problem.Extensions[key] = value;
            }
        }

        return result;
    }

    protected static IdentityResponse ToResponse(AuthenticatedIdentity identity) => new(
        identity.UserId,
        identity.EmployeeId,
        identity.Email,
        identity.DisplayName,
        identity.Roles,
        identity.Permissions.Order(StringComparer.Ordinal).ToList(),
        identity.DataScopeClaim,
        identity.DataScope.DepartmentIds.Order().ToList(),
        identity.PasswordChangeRequired);

    protected static SessionResponse ToResponse(AuthenticationSession session, DateTimeOffset now) => new(
        session.AccessToken.Value,
        IssuedAccessToken.BearerTokenType,
        session.AccessToken.ExpiresInSeconds(now),
        session.AccessToken.ExpiresAt,
        session.RefreshToken,
        session.RefreshTokenExpiresAt,
        ToResponse(session.Identity));
}

/// <summary>OpenAPI AuthenticatedIdentity — who the caller is and what they may do.</summary>
public sealed record IdentityResponse(
    long UserId,
    long? EmployeeId,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string DataScope,
    IReadOnlyList<long> DepartmentIds,
    bool PasswordChangeRequired);

/// <summary>
/// OpenAPI Session. <c>refreshToken</c> is absent while a password change is pending: that session may do
/// nothing but change the password, so it is deliberately not renewable.
/// </summary>
public sealed record SessionResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    DateTimeOffset ExpiresAt,
    string? RefreshToken,
    DateTimeOffset? RefreshTokenExpiresAt,
    IdentityResponse User);
