using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Users;

namespace Qlns.Api.Modules.Identity.Users;

/// <summary>
/// ADM-02: account administration. Mutating endpoints are optimistic-concurrency controlled through
/// <c>If-Match</c> on users.version, exactly like the business modules, so two administrators editing the
/// same account cannot silently overwrite each other.
/// </summary>
[Route("api/v1/admin/users")]
public sealed class UserAccountsController(UserAccountService service) : IdentityControllerBase
{
    [HttpGet(Name = "listUserAccounts")]
    [Authorize(Policy = IdentityPolicies.UserRead)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? roleCode = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return Task.FromResult<IActionResult>(InvalidPage());
        }

        UserAccountStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!UserAccountStatusNames.TryParseContract(status, out var parsed))
            {
                return Task.FromResult<IActionResult>(
                    ValidationProblemResult("status", "Unknown account status. Use active or disabled."));
            }

            statusFilter = parsed;
        }

        var query = new UserAccountSearchQuery(search, statusFilter, roleCode, pageRequest);

        return ExecuteIdentityAsync(async actor =>
        {
            var result = await service.SearchAsync(query, actor, cancellationToken);
            return Ok(ToPage(result, ToResponse));
        });
    }

    [HttpGet("{userId:long:min(1)}", Name = "getUserAccount")]
    [Authorize(Policy = IdentityPolicies.UserRead)]
    public Task<IActionResult> Get(long userId, CancellationToken cancellationToken) =>
        ExecuteIdentityAsync(async actor =>
        {
            var view = await service.GetAsync(userId, actor, cancellationToken);
            SetETag(view.Account.Version);
            return Ok(ToResponse(view));
        });

    [HttpPost(Name = "createUserAccount")]
    [Authorize(Policy = IdentityPolicies.UserManage)]
    [ProducesResponseType<UserAccountResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        [FromBody] CreateUserAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseGrants(request.Roles, out var grants, out var invalidScope))
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("roles",
                $"Unknown dataScopeType '{invalidScope}'. Use self, department or organization."));
        }

        return ExecuteIdentityAsync(async actor =>
        {
            var view = await service.CreateAsync(
                new CreateUserAccountCommand(
                    new UserAccountWrite(request.Email, request.DisplayName),
                    request.InitialPassword,
                    request.EmployeeId,
                    grants,
                    actor,
                    CorrelationId),
                cancellationToken);

            SetETag(view.Account.Version);
            return CreatedAtRoute("getUserAccount", new { userId = view.Account.Id }, ToResponse(view));
        });
    }

    [HttpPut("{userId:long:min(1)}", Name = "updateUserAccount")]
    [Authorize(Policy = IdentityPolicies.UserManage)]
    public Task<IActionResult> Update(
        long userId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] UserAccountWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        return ExecuteIdentityAsync(async actor =>
        {
            var view = await service.UpdateAsync(
                new UpdateUserAccountCommand(
                    userId, expectedVersion, new UserAccountWrite(request.Email, request.DisplayName), actor),
                cancellationToken);

            SetETag(view.Account.Version);
            return Ok(ToResponse(view));
        });
    }

    /// <summary>
    /// Enables or disables an account. Disabling revokes every refresh token of that account, so the session
    /// cannot outlive the decision by more than one access-token lifetime.
    /// </summary>
    [HttpPost("{userId:long:min(1)}/{accountAction:regex(^(enable|disable)$)}", Name = "setUserAccountStatus")]
    [Authorize(Policy = IdentityPolicies.UserManage)]
    public Task<IActionResult> SetStatus(
        long userId,
        string accountAction,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        var status = string.Equals(accountAction, "disable", StringComparison.Ordinal)
            ? UserAccountStatus.Disabled
            : UserAccountStatus.Active;

        return ExecuteIdentityAsync(async actor =>
        {
            var view = await service.SetStatusAsync(
                new SetUserAccountStatusCommand(userId, expectedVersion, status, actor), cancellationToken);

            SetETag(view.Account.Version);
            return Ok(ToResponse(view));
        });
    }

    /// <summary>
    /// Administrative password reset. The new secret must be handed to its owner out of band; the account is
    /// forced to replace it at the next sign-in and all of its sessions are dropped.
    /// </summary>
    [HttpPost("{userId:long:min(1)}/password-reset", Name = "resetUserPassword")]
    [Authorize(Policy = IdentityPolicies.UserManage)]
    public Task<IActionResult> ResetPassword(
        long userId,
        [FromBody] ResetUserPasswordRequest request,
        CancellationToken cancellationToken) =>
        ExecuteIdentityAsync(async actor =>
        {
            var view = await service.ResetPasswordAsync(
                new ResetUserPasswordCommand(userId, request.NewPassword, actor), cancellationToken);

            SetETag(view.Account.Version);
            return Ok(ToResponse(view));
        });

    /// <summary>Replaces the account's role grants. An empty list leaves it authenticated but unauthorized.</summary>
    [HttpPut("{userId:long:min(1)}/roles", Name = "replaceUserRoleGrants")]
    [Authorize(Policy = IdentityPolicies.UserManage)]
    public Task<IActionResult> ReplaceRoles(
        long userId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] RoleGrantsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        if (!TryParseGrants(request.Roles, out var grants, out var invalidScope))
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("roles",
                $"Unknown dataScopeType '{invalidScope}'. Use self, department or organization."));
        }

        return ExecuteIdentityAsync(async actor =>
        {
            var view = await service.ReplaceRoleGrantsAsync(
                new ReplaceRoleGrantsCommand(userId, expectedVersion, grants, actor), cancellationToken);

            SetETag(view.Account.Version);
            return Ok(ToResponse(view));
        });
    }

    private static bool TryParseGrants(
        IReadOnlyList<RoleGrantRequest>? requests,
        out IReadOnlyList<RoleGrant> grants,
        out string? invalidScope)
    {
        invalidScope = null;
        var parsed = new List<RoleGrant>();

        foreach (var request in requests ?? [])
        {
            if (!DataScopeTypeNames.TryParseContract(request.DataScopeType, out var scopeType))
            {
                invalidScope = request.DataScopeType;
                grants = [];
                return false;
            }

            parsed.Add(new RoleGrant(request.RoleCode.Trim(), scopeType, request.DataScopeId ?? 0));
        }

        grants = parsed;
        return true;
    }

    private static UserAccountResponse ToResponse(UserAccountView view) => new(
        view.Account.Id,
        view.Account.Email,
        view.Account.DisplayName,
        view.Account.Status.ToContract(),
        view.Account.ExternalSubject,
        view.Account.EmployeeId,
        view.Grants
            .Select(grant => new RoleGrantResponse(grant.RoleCode, grant.ScopeType.ToContract(), grant.ScopeId))
            .ToList(),
        view.DataScope.OrganizationWide ? DataScopeTypeNames.Organization
            : view.DataScope.DepartmentIds.Count > 0 ? DataScopeTypeNames.Department
            : DataScopeTypeNames.Self,
        view.HasCredential,
        view.MustChangePassword,
        view.PasswordUpdatedAt,
        view.LastLoginAt,
        view.LockedUntil,
        view.Account.CreatedAt,
        view.Account.UpdatedAt,
        view.Account.Version);
}

/// <summary>OpenAPI RoleGrant.</summary>
public sealed record RoleGrantRequest(
    [Required, MaxLength(80)] string RoleCode,
    [Required] string DataScopeType,
    long? DataScopeId);

/// <summary>OpenAPI UserAccountWrite.</summary>
public sealed record UserAccountWriteRequest(
    [Required, MaxLength(320)] string Email,
    [Required, MaxLength(UserAccount.DisplayNameMaxLength)] string DisplayName);

/// <summary>OpenAPI CreateUserAccountRequest. The initial password always requires a change at first sign-in.</summary>
public sealed record CreateUserAccountRequest(
    [Required, MaxLength(320)] string Email,
    [Required, MaxLength(UserAccount.DisplayNameMaxLength)] string DisplayName,
    [Required, MaxLength(PasswordPolicy.MaxLength)] string InitialPassword,
    long? EmployeeId,
    IReadOnlyList<RoleGrantRequest>? Roles);

/// <summary>OpenAPI ResetUserPasswordRequest.</summary>
public sealed record ResetUserPasswordRequest(
    [Required, MaxLength(PasswordPolicy.MaxLength)] string NewPassword);

/// <summary>OpenAPI RoleGrantsRequest — the complete, replacing set of grants.</summary>
public sealed record RoleGrantsRequest([Required] IReadOnlyList<RoleGrantRequest> Roles);

/// <summary>OpenAPI RoleGrant as returned.</summary>
public sealed record RoleGrantResponse(string RoleCode, string DataScopeType, long DataScopeId);

/// <summary>OpenAPI UserAccount. The password hash and the refresh tokens are never exposed.</summary>
public sealed record UserAccountResponse(
    long Id,
    string Email,
    string DisplayName,
    string Status,
    string ExternalSubject,
    long? EmployeeId,
    IReadOnlyList<RoleGrantResponse> Roles,
    string DataScope,
    bool HasCredential,
    bool MustChangePassword,
    DateTimeOffset? PasswordUpdatedAt,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset? LockedUntil,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    long Version);
