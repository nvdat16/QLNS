using System.Globalization;
using System.Security.Claims;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Shared;

/// <summary>
/// Maps the validated JWT principal to a <see cref="CoreHrActor"/>.
/// Claims: <c>qlns_user_id</c> (required), <c>qlns_employee_id</c>, <c>data_scope</c>
/// (self | department | organization), <c>department_id</c> (repeatable), <c>permission</c> (repeatable).
/// </summary>
public static class CoreHrActorResolver
{
    public static bool TryResolve(ClaimsPrincipal user, string correlationId, out CoreHrActor actor)
    {
        actor = null!;
        if (!TryParseId(user.FindFirstValue("qlns_user_id"), out var userId))
        {
            return false;
        }

        long? employeeId = TryParseId(user.FindFirstValue("qlns_employee_id"), out var parsedEmployee)
            ? parsedEmployee
            : null;

        var organizationWide = user.HasClaim("data_scope", "organization");
        var departmentIds = user.FindAll("department_id")
            .Select(claim => TryParseId(claim.Value, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        var permissions = user.FindAll("permission")
            .Select(claim => claim.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        actor = new CoreHrActor(
            userId,
            employeeId,
            new CoreHrDataScope(organizationWide, departmentIds),
            permissions,
            correlationId);
        return true;
    }

    private static bool TryParseId(string? value, out long id) =>
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id) && id > 0;
}
