using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;

namespace Qlns.DataAccess.Modules.Recruitment.Interviews;

/// <summary>Registers the Recruitment → Interviews feature (repository and business service). Call from the host composition root.</summary>
public static class InterviewsRegistration
{
    public static IServiceCollection AddRecruitmentInterviews(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IInterviewRepository, InterviewRepository>();
        services.AddScoped<InterviewService>();
        return services;
    }
}
