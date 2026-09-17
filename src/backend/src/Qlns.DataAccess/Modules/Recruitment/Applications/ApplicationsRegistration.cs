using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.Recruitment.Applications;

/// <summary>Registers the Recruitment → Pipeline feature (REC-03): service and repository. Call from the host composition root.</summary>
public static class ApplicationsRegistration
{
    public static IServiceCollection AddRecruitmentApplications(this IServiceCollection services)
    {
        services.AddScoped<IRecruitmentApplicationRepository, RecruitmentApplicationRepository>();
        services.AddScoped<RecruitmentPipelineService>();
        return services;
    }
}
