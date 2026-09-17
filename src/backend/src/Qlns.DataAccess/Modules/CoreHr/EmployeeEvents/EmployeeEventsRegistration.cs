using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

namespace Qlns.DataAccess.Modules.CoreHr.EmployeeEvents;

public static class EmployeeEventsRegistration
{
    /// <summary>Registers the Core HR → Employee Events feature (repository and business service).</summary>
    public static IServiceCollection AddCoreHrEmployeeEvents(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeEventRepository, EmployeeEventRepository>();
        services.AddScoped<EmployeeMovementService>();
        return services;
    }
}
