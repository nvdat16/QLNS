using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.DataAccess.Modules.Contracts.Addenda;

namespace Qlns.DataAccess.Modules.Contracts.Contracts;

/// <summary>
/// Composition of the Contracts module (employment contracts and addenda). Requires <c>AddDataAccess</c>
/// (for <see cref="QlnsDbContext"/>), a registered <see cref="TimeProvider"/> and the document ports
/// <c>IDocumentStorage</c> / <c>IMalwareScanner</c> registered by <c>AddCoreHrEmployeeDocuments</c>.
/// </summary>
public static class ContractsRegistration
{
    public static IServiceCollection AddContracts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<SignedDocumentUploader>();
        services.AddScoped<IContractRepository, ContractRepository>();
        services.AddScoped<ContractService>();
        services.AddScoped<IContractAddendumRepository, ContractAddendumRepository>();
        services.AddScoped<ContractAddendumService>();
        return services;
    }
}
