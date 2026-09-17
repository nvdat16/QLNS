using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;
using Qlns.DataAccess.Modules.CoreHr.EmployeeEvents;
using Qlns.DataAccess.Modules.CoreHr.Employees;
using Qlns.DataAccess.Modules.CoreHr.Onboarding;
using Qlns.DataAccess.Modules.CoreHr.Organization;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Qlns")
            ?? throw new InvalidOperationException("ConnectionStrings:Qlns is required.");

        services.AddDbContext<QlnsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IRecruitmentApplicationRepository, RecruitmentApplicationRepository>();

        // Core HR — each feature registers its repository implementations and business service.
        services.AddCoreHrEmployees();
        services.AddCoreHrOrganization();
        services.AddCoreHrOnboarding();
        services.AddCoreHrEmployeeEvents();
        services.AddCoreHrEmployeeDocuments(configuration);
        return services;
    }
}
