using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;

namespace Qlns.DataAccess.Modules.Recruitment.Intake;

/// <summary>Composition of the Recruitment → Intake feature (REC-02).</summary>
public static class IntakeRegistration
{
    /// <summary>
    /// Registers the intake service, repository and the development résumé parser. Requires <c>AddDataAccess</c>
    /// (for <see cref="QlnsDbContext"/>), a registered <see cref="TimeProvider"/> and the document storage and malware
    /// scanner ports registered by <c>AddCoreHrEmployeeDocuments</c> (<c>IDocumentStorage</c>, <c>IMalwareScanner</c>).
    /// </summary>
    public static IServiceCollection AddRecruitmentIntake(this IServiceCollection services)
    {
        services.AddSingleton<IResumeParser, DevelopmentOnlyResumeParser>();
        services.AddScoped<ICandidateIntakeRepository, CandidateIntakeRepository>();
        services.AddScoped<CandidateIntakeService>();
        return services;
    }
}
