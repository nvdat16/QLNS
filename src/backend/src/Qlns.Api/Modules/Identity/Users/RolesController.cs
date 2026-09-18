using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Users;

namespace Qlns.Api.Modules.Identity.Users;

/// <summary>
/// The role catalogue (ADM-02). Read-only on purpose: roles and their permissions are reference data
/// deployed with database/seed_roles.sql, so that a change of the permission matrix goes through review
/// rather than through a live API call.
/// </summary>
[Route("api/v1/admin/roles")]
public sealed class RolesController(UserAccountService service) : IdentityControllerBase
{
    [HttpGet(Name = "listRoles")]
    [Authorize(Policy = IdentityPolicies.RoleRead)]
    [ProducesResponseType<IReadOnlyList<RoleCatalogResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List(CancellationToken cancellationToken) =>
        ExecuteIdentityAsync(async actor =>
        {
            var catalogue = await service.GetRoleCatalogAsync(actor, cancellationToken);
            return Ok(catalogue.Select(ToResponse).ToList());
        });

    private static RoleCatalogResponse ToResponse(RoleCatalogEntry entry) => new(
        entry.Code,
        entry.Name,
        entry.Description,
        entry.IsAssignable,
        entry.Permissions);
}

/// <summary>OpenAPI Role.</summary>
public sealed record RoleCatalogResponse(
    string Code,
    string Name,
    string? Description,
    bool IsAssignable,
    IReadOnlyList<string> Permissions);
