using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

namespace Qlns.DataAccess.Modules.CoreHr.Offboarding;

/// <summary>Registers the Core HR → Offboarding feature (EMP-07): repositories and business services. Call from the host composition root.</summary>
public static class OffboardingRegistration
{
    public static IServiceCollection AddCoreHrOffboarding(this IServiceCollection services)
    {
        services.AddScoped<IOffboardingCaseRepository, OffboardingCaseRepository>();
        services.AddScoped<IOffboardingTaskRepository, OffboardingTaskRepository>();
        services.AddScoped<OffboardingCaseService>();
        services.AddScoped<OffboardingTaskService>();
        return services;
    }
}
