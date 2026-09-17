using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.Organization;

namespace Qlns.DataAccess.Modules.CoreHr.Organization;

/// <summary>
/// Wires the Core HR Organization feature (EMP-02). Call after <c>AddDataAccess</c> so the
/// <c>QlnsDbContext</c> and <c>TimeProvider</c> registrations are available.
/// </summary>
public static class OrganizationRegistration
{
    public static IServiceCollection AddCoreHrOrganization(this IServiceCollection services)
    {
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();
        services.AddScoped<OrganizationService>();
        return services;
    }
}
