using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

namespace Qlns.DataAccess.Modules.CoreHr.Onboarding;

/// <summary>Registers the Core HR → Onboarding feature (service and repository). Call from the host composition root.</summary>
public static class OnboardingRegistration
{
    public static IServiceCollection AddCoreHrOnboarding(this IServiceCollection services)
    {
        services.AddScoped<IOnboardingTaskRepository, OnboardingTaskRepository>();
        services.AddScoped<OnboardingTaskService>();
        return services;
    }
}
