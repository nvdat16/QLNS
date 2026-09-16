using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
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
        return services;
    }
}
