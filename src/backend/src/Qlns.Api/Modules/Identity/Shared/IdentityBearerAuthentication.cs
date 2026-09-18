using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Qlns.Api.Development;
using Qlns.DataAccess.Modules.Identity.Authentication;

namespace Qlns.Api.Modules.Identity.Shared;

/// <summary>
/// Configures the bearer handler to validate the access tokens this API issues itself
/// (<see cref="JwtAccessTokenIssuer"/>). Both sides read the same <see cref="JwtAuthenticationOptions"/>, so
/// the signing key, issuer and audience cannot drift apart.
/// <para>
/// In Development the handler additionally trusts the <c>/dev/token</c> issuer when a development signing key
/// is configured, so the persona shortcut and real sign-in coexist on one host. Outside Development that key
/// is ignored (<see cref="DevelopmentAuthentication.IsEnabled"/>), leaving exactly one accepted issuer.
/// </para>
/// </summary>
public static class IdentityBearerAuthentication
{
    public static void ConfigureJwtBearer(
        JwtBearerOptions options,
        JwtAuthenticationOptions jwt,
        IHostEnvironment environment,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(jwt);

        var issuers = new List<string> { jwt.Issuer };
        var keys = new List<SecurityKey> { new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)) };

        if (DevelopmentAuthentication.IsEnabled(environment, configuration))
        {
            issuers.Add(DevelopmentAuthentication.Issuer);
            keys.Add(DevelopmentAuthentication.SigningKey(configuration));
        }

        options.Authority = null;
        options.RequireHttpsMetadata = !environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuers = issuers,
            ValidAudience = jwt.Audience,
            IssuerSigningKeys = keys,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }
}
