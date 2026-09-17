using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;

namespace Qlns.DataAccess.Modules.CoreHr.Probation;

/// <summary>Registers the Core HR → Probation feature (EMP-06): repository and business service. Call from the host composition root.</summary>
public static class ProbationRegistration
{
    public static IServiceCollection AddCoreHrProbation(this IServiceCollection services)
    {
        services.AddScoped<IProbationReviewRepository, ProbationReviewRepository>();
        services.AddScoped<ProbationReviewService>();
        return services;
    }
}
