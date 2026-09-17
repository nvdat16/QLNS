using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.DataAccess.Modules.Recruitment.Requisitions;

/// <summary>Registers the Recruitment → Requisitions feature (REC-01): service and repository. Call from the host composition root.</summary>
public static class RequisitionsRegistration
{
    public static IServiceCollection AddRecruitmentRequisitions(this IServiceCollection services)
    {
        services.AddScoped<IRequisitionRepository, RequisitionRepository>();
        services.AddScoped<RequisitionService>();
        return services;
    }
}
