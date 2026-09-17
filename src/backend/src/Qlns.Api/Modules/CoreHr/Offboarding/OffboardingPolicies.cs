using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

namespace Qlns.Api.Modules.CoreHr.Offboarding;

/// <summary>
/// Endpoint-level authorization policies of Core HR → Offboarding (EMP-07). <see cref="Write"/> accepts the
/// approve claim too because the case-transition route hosts approve next to start/complete/cancel; the services
/// re-check the exact claim per action and enforce data scope.
/// </summary>
public static class OffboardingPolicies
{
    public const string Read = "CoreHrOffboardingRead";
    public const string Write = "CoreHrOffboardingWrite";

    public static void AddOffboardingPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.AddPolicy(Read, p => p.RequireClaim("permission", OffboardingPermissions.Read));
        options.AddPolicy(Write, p => p.RequireClaim("permission",
            OffboardingPermissions.Write, OffboardingPermissions.Approve));
    }
}
