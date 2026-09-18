using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qlns.DataAccess.Modules.Contracts.Contracts;
using Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;
using Qlns.DataAccess.Modules.CoreHr.EmployeeEvents;
using Qlns.DataAccess.Modules.CoreHr.Employees;
using Qlns.DataAccess.Modules.CoreHr.Offboarding;
using Qlns.DataAccess.Modules.CoreHr.Onboarding;
using Qlns.DataAccess.Modules.CoreHr.Organization;
using Qlns.DataAccess.Modules.CoreHr.Probation;
using Qlns.DataAccess.Modules.Identity.Authentication;
using Qlns.DataAccess.Modules.Recruitment.Applications;
using Qlns.DataAccess.Modules.Recruitment.Evaluations;
using Qlns.DataAccess.Modules.Recruitment.Intake;
using Qlns.DataAccess.Modules.Recruitment.Interviews;
using Qlns.DataAccess.Modules.Recruitment.Offers;
using Qlns.DataAccess.Modules.Recruitment.Requisitions;

namespace Qlns.DataAccess;

/// <summary>
/// Composition root of the data layer. Each feature registers its own repositories, business service and
/// adapters through an <c>Add&lt;Module&gt;&lt;Feature&gt;</c> extension so the host only knows module names.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Qlns")
            ?? throw new InvalidOperationException("ConnectionStrings:Qlns is required.");

        services.AddDbContext<QlnsDbContext>(options => options.UseNpgsql(connectionString));

        // Identity & Access (ADM-01 … ADM-02). Registered first: every other module depends on the actor it
        // establishes, and a missing signing key must stop start-up before any business service is wired.
        services.AddIdentityAccess(configuration);

        // Core HR — EMP-01 … EMP-07. Employee Documents also provides IDocumentStorage / IMalwareScanner
        // for every other feature that stores files (résumés, signed contracts), so it is registered first.
        services.AddCoreHrEmployees();
        services.AddCoreHrOrganization();
        services.AddCoreHrOnboarding();
        services.AddCoreHrEmployeeEvents();
        services.AddCoreHrEmployeeDocuments(configuration);
        services.AddCoreHrProbation();
        services.AddCoreHrOffboarding();

        // Core HR — Contracts (CON-01 … CON-03).
        services.AddContracts();

        // Recruitment — REC-01 … REC-06.
        services.AddRecruitmentRequisitions();
        services.AddRecruitmentIntake();
        services.AddRecruitmentApplications();
        services.AddRecruitmentInterviews();
        services.AddRecruitmentEvaluations();
        services.AddRecruitmentOffers(configuration);
        return services;
    }
}
