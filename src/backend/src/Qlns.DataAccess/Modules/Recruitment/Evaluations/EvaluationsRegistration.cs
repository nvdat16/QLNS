using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

namespace Qlns.DataAccess.Modules.Recruitment.Evaluations;

/// <summary>
/// Registers the Recruitment → Evaluations feature: repository, business service and the default equal-weight
/// scoring policy (replace the <see cref="IEvaluationScoringPolicy"/> registration to apply per-position weights).
/// </summary>
public static class EvaluationsRegistration
{
    public static IServiceCollection AddRecruitmentEvaluations(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IEvaluationScoringPolicy, EqualWeightScoringPolicy>();
        services.AddScoped<IEvaluationRepository, EvaluationRepository>();
        services.AddScoped<EvaluationService>();
        return services;
    }
}
