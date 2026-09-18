using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Users;
using Qlns.DataAccess.Modules.Identity.Users;

namespace Qlns.DataAccess.Modules.Identity.Authentication;

/// <summary>Composition of the Identity &amp; Access module (ADM-01 sign-in, ADM-02 account administration).</summary>
public static class IdentityRegistration
{
    /// <summary>
    /// Registers password hashing, the token issuer, both repositories and both business services.
    /// Requires <c>Authentication:Jwt:SigningKey</c>: without an in-house signing key the module cannot
    /// issue the tokens it validates, so the host fails fast instead of starting a half-configured API.
    /// </summary>
    public static IServiceCollection AddIdentityAccess(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = JwtAuthenticationOptions.Require(configuration);

        services.AddSingleton(options);
        services.AddSingleton(options.Settings);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IRefreshTokenGenerator, RandomRefreshTokenGenerator>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
        services.AddScoped<AuthenticationService>();

        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<UserAccountService>();
        return services;
    }
}
