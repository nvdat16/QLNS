using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;

namespace Qlns.Api.Modules.Identity.Authentication;

/// <summary>
/// ADM-01: the sign-in surface. <c>login</c>, <c>refresh</c> and <c>logout</c> are anonymous — they are how a
/// caller obtains an identity in the first place — while <c>me</c> and <c>change-password</c> require the
/// bearer token. Rate limiting belongs in front of this controller (reverse proxy or gateway); what the
/// application itself guarantees is the per-account lockout in <see cref="SignInLockout"/>.
/// </summary>
[Route("api/v1/auth")]
public sealed class AuthController(AuthenticationService service, TimeProvider timeProvider) : IdentityControllerBase
{
    [AllowAnonymous]
    [HttpPost("login", Name = "signIn")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken) =>
        ExecuteAnonymousAsync(async () =>
        {
            var session = await service.SignInAsync(
                new SignInCommand(request.Email, request.Password, SignInContext), cancellationToken);

            return Ok(ToResponse(session, timeProvider.GetUtcNow()));
        });

    /// <summary>
    /// Exchanges a refresh token for a new pair. The presented token is consumed even when the caller never
    /// receives the answer, so a client must always store the token it gets back.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("refresh", Name = "refreshSession")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        ExecuteAnonymousAsync(async () =>
        {
            var session = await service.RefreshAsync(
                new RefreshSessionCommand(request.RefreshToken, SignInContext), cancellationToken);

            return Ok(ToResponse(session, timeProvider.GetUtcNow()));
        });

    /// <summary>
    /// Revokes the refresh token. Always answers 204, whether or not the token existed: reporting the
    /// difference would turn sign-out into an oracle for guessing valid tokens. The access token stays valid
    /// until it expires — that is the documented cost of stateless bearer tokens.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("logout", Name = "signOut")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
        ExecuteAnonymousAsync(async () =>
        {
            await service.SignOutAsync(new SignOutCommand(request.RefreshToken, SignInContext), cancellationToken);
            return NoContent();
        });

    [Authorize]
    [HttpGet("me", Name = "getCurrentIdentity")]
    [ProducesResponseType<IdentityResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Me(CancellationToken cancellationToken) =>
        ExecuteIdentityAsync(async actor =>
        {
            var identity = await service.DescribeAsync(actor, cancellationToken);
            return Ok(ToResponse(identity));
        });

    /// <summary>
    /// Self-service password change. Available to every authenticated actor, including the restricted
    /// session issued when a password reset is pending — that is the only call such a session can make.
    /// </summary>
    [Authorize]
    [HttpPost("change-password", Name = "changeOwnPassword")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken) =>
        ExecuteIdentityAsync(async actor =>
        {
            await service.ChangeOwnPasswordAsync(
                new ChangeOwnPasswordCommand(request.CurrentPassword, request.NewPassword, actor, SignInContext),
                cancellationToken);

            return NoContent();
        });
}

/// <summary>OpenAPI LoginRequest.</summary>
public sealed record LoginRequest(
    [Required, MaxLength(320)] string Email,
    [Required, MaxLength(PasswordPolicy.MaxLength)] string Password);

/// <summary>OpenAPI RefreshTokenRequest, shared by refresh and logout.</summary>
public sealed record RefreshTokenRequest([Required, MaxLength(512)] string RefreshToken);

/// <summary>OpenAPI ChangePasswordRequest.</summary>
public sealed record ChangePasswordRequest(
    [Required, MaxLength(PasswordPolicy.MaxLength)] string CurrentPassword,
    [Required, MaxLength(PasswordPolicy.MaxLength)] string NewPassword);
