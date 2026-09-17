using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;

namespace Qlns.DataAccess.Modules.Recruitment.Offers;

/// <summary>Composition of the Recruitment → Offers feature (REC-06.1 / REC-06.2).</summary>
public static class OffersRegistration
{
    /// <summary>
    /// Registers the offer service, repository and the HMAC response-token adapter bound to
    /// <c>Recruitment:OfferTokenSigningKey</c>. Fails fast when the key is missing.
    /// Requires <c>AddDataAccess</c> (for <see cref="QlnsDbContext"/>) and a registered <see cref="TimeProvider"/>.
    /// </summary>
    public static IServiceCollection AddRecruitmentOffers(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var signingKey = configuration.GetSection(OfferTokenOptions.SectionName)[OfferTokenOptions.SigningKeyName];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException(
                $"{OfferTokenOptions.SectionName}:{OfferTokenOptions.SigningKeyName} is required to sign candidate offer-response tokens. " +
                $"Set it in configuration (user secrets or environment variable {OfferTokenOptions.SectionName}__{OfferTokenOptions.SigningKeyName}).");
        }

        services.AddSingleton(new OfferTokenOptions { SigningKey = signingKey });
        services.AddSingleton<IOfferResponseTokenService, HmacOfferResponseTokenService>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<OfferService>();
        return services;
    }
}
