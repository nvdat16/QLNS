using System.Globalization;
using Microsoft.Extensions.Configuration;
using Qlns.BusinessLogic.Modules.Identity.Authentication;

namespace Qlns.DataAccess.Modules.Identity.Authentication;

/// <summary>
/// Configuration of the in-house token issuer, section <c>Authentication:Jwt</c>. Presence of
/// <see cref="SigningKey"/> is what switches the host from "validate tokens of an external OIDC provider"
/// to "issue and validate our own": both the issuer in this assembly and the JWT bearer handler in the host
/// read this one object, so signing and validation can never drift apart.
/// </summary>
public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Authentication";
    public const string JwtSectionName = "Jwt";
    public const string SigningKeyName = "SigningKey";

    /// <summary>HS256 needs a key of at least the hash size; anything shorter is rejected outright.</summary>
    public const int MinSigningKeyLength = 32;

    public const string DefaultIssuer = "qlns";

    /// <summary>HMAC secret of the access token (<c>Authentication:Jwt:SigningKey</c>). Never logged.</summary>
    public required string SigningKey { get; init; }

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public AuthenticationSettings Settings { get; init; } = new();

    /// <summary>
    /// Reads the section, failing fast when the signing key is absent. The key is not optional: the ADM module
    /// issues the tokens this host validates, so a host without it could authorize requests but never let
    /// anyone sign in — a half-configured API that looks healthy is worse than one that refuses to start.
    /// </summary>
    public static JwtAuthenticationOptions Require(IConfiguration configuration) =>
        TryRead(configuration) ?? throw new InvalidOperationException(
            $"{SectionName}:{JwtSectionName}:{SigningKeyName} is required to issue and validate access tokens. " +
            $"Set it in user secrets or in the environment variable {SectionName}__{JwtSectionName}__{SigningKeyName} " +
            $"(at least {MinSigningKeyLength} characters).");

    /// <summary>
    /// Reads the section and returns null when <c>Authentication:Jwt:SigningKey</c> is absent. Prefer
    /// <see cref="Require"/> in the composition root; this overload exists for diagnostics.
    /// </summary>
    public static JwtAuthenticationOptions? TryRead(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(SectionName).GetSection(JwtSectionName);
        var signingKey = section[SigningKeyName];
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return null;
        }

        if (signingKey.Length < MinSigningKeyLength)
        {
            throw new InvalidOperationException(
                $"{SectionName}:{JwtSectionName}:{SigningKeyName} must be at least {MinSigningKeyLength} characters long. " +
                $"Set it through user secrets or the environment variable {SectionName}__{JwtSectionName}__{SigningKeyName}.");
        }

        var audience = section["Audience"] ?? configuration.GetSection(SectionName)["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException($"{SectionName}:Audience is required to issue access tokens.");
        }

        var settings = new AuthenticationSettings
        {
            AccessTokenLifetime = Minutes(section, "AccessTokenMinutes", 30),
            PasswordChangeTokenLifetime = Minutes(section, "PasswordChangeTokenMinutes", 10),
            RefreshTokenLifetime = TimeSpan.FromDays(Number(section, "RefreshTokenDays", 14))
        };
        settings.Validate();

        return new JwtAuthenticationOptions
        {
            SigningKey = signingKey,
            Issuer = section["Issuer"] ?? DefaultIssuer,
            Audience = audience,
            Settings = settings
        };
    }

    private static TimeSpan Minutes(IConfiguration section, string key, double fallback) =>
        TimeSpan.FromMinutes(Number(section, key, fallback));

    private static double Number(IConfiguration section, string key, double fallback)
    {
        var value = section[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) || parsed <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{JwtSectionName}:{key} must be a positive number.");
        }

        return parsed;
    }
}
