using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

namespace Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;

/// <summary>Composition of the Core HR → Employee Documents feature (EMP-05).</summary>
public static class EmployeeDocumentsRegistration
{
    /// <summary>
    /// Registers the document service, repository and the development storage/scanner adapters bound to
    /// configuration section <c>Documents</c> (<c>StorageRoot</c>, <c>PublicBaseUrl</c>, <c>SigningKey</c>).
    /// Requires <c>AddDataAccess</c> (for <see cref="QlnsDbContext"/>) and a registered <see cref="TimeProvider"/>.
    /// </summary>
    public static IServiceCollection AddCoreHrEmployeeDocuments(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(DocumentStorageOptions.SectionName);

        var signingKey = section["SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                "Documents:SigningKey is required to sign employee-document download URLs. " +
                "Set it in configuration (user secrets or environment variable Documents__SigningKey).");
        }

        var storageRoot = section["StorageRoot"];
        if (string.IsNullOrWhiteSpace(storageRoot))
        {
            storageRoot = Path.Combine(Directory.GetCurrentDirectory(), DocumentStorageOptions.DefaultStorageFolderName);
        }

        var publicBaseUrl = section["PublicBaseUrl"];
        if (string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            publicBaseUrl = DocumentStorageOptions.DefaultPublicBaseUrl;
        }

        var options = new DocumentStorageOptions
        {
            StorageRoot = Path.GetFullPath(storageRoot),
            PublicBaseUrl = publicBaseUrl.TrimEnd('/'),
            SigningKey = signingKey
        };

        services.AddSingleton(options);
        services.AddSingleton<IDocumentStorage, FileSystemDocumentStorage>();
        services.AddSingleton<IMalwareScanner, DevelopmentOnlyMalwareScanner>();
        services.AddScoped<IEmployeeDocumentRepository, EmployeeDocumentRepository>();
        services.AddScoped<EmployeeDocumentService>();
        return services;
    }
}
