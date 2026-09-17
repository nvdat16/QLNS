using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.Employees;

namespace Qlns.DataAccess.Modules.CoreHr.Employees;

/// <summary>Composition of the Core HR → Employees feature: call from the host after AddDataAccess.</summary>
public static class EmployeesRegistration
{
    public static IServiceCollection AddCoreHrEmployees(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<EmployeeDirectoryService>();
        return services;
    }
}
