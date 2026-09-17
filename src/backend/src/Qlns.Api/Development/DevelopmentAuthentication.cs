using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Development;

/// <summary>
/// DEVELOPMENT ONLY. Replaces the external Identity Provider with a symmetric signing key and a
/// <c>GET /dev/token?persona=…</c> endpoint that mints tokens for the personas in database/seed_dev.sql.
/// Active only when the environment is Development AND <c>Authentication:DevelopmentSigningKey</c> is set.
/// </summary>
public static class DevelopmentAuthentication
{
    public const string Issuer = "qlns-dev";

    public static bool IsEnabled(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() &&
        !string.IsNullOrWhiteSpace(configuration["Authentication:DevelopmentSigningKey"]);

    public static void ConfigureJwtBearer(JwtBearerOptions options, IConfiguration configuration)
    {
        options.Authority = null;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = configuration["Authentication:Audience"],
            IssuerSigningKey = SigningKey(configuration),
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public static void MapDevelopmentTokenEndpoint(this WebApplication app)
    {
        app.MapGet("/dev/token", (string? persona, IConfiguration configuration) =>
        {
            if (persona is null || !Personas.TryGetValue(persona, out var claims))
            {
                return Results.Problem(
                    title: "Unknown persona",
                    detail: $"Use one of: {string.Join(", ", Personas.Keys)}",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var handler = new JsonWebTokenHandler();
            var token = handler.CreateToken(new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = configuration["Authentication:Audience"],
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(SigningKey(configuration), SecurityAlgorithms.HmacSha256)
            });

            return Results.Ok(new { persona, token, expiresInSeconds = 8 * 3600 });
        }).AllowAnonymous().ExcludeFromDescription();
    }

    private static SymmetricSecurityKey SigningKey(IConfiguration configuration) =>
        new(Encoding.UTF8.GetBytes(configuration["Authentication:DevelopmentSigningKey"]!));

    private static readonly string[] AllCoreHrPermissions =
    [
        CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeReadSensitive,
        CoreHrPermissions.EmployeeProfileUpdate, CoreHrPermissions.EmployeeProfileManage,
        CoreHrPermissions.OrganizationRead, CoreHrPermissions.OrganizationManage,
        CoreHrPermissions.OnboardingRead, CoreHrPermissions.OnboardingManage, CoreHrPermissions.OnboardingReopen,
        CoreHrPermissions.EventRead, CoreHrPermissions.EventWrite, CoreHrPermissions.EventApprove,
        CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentReadSensitive, CoreHrPermissions.DocumentUpload
    ];

    /// <summary>Personas match users/employees in database/seed_dev.sql.</summary>
    private static readonly Dictionary<string, Claim[]> Personas = new(StringComparer.OrdinalIgnoreCase)
    {
        ["hr-manager"] = Build(userId: 1, employeeId: 2, scope: "organization", departments: [], AllCoreHrPermissions),
        ["hr-officer"] = Build(userId: 2, employeeId: 6, scope: "organization", departments: [],
            AllCoreHrPermissions.Except([CoreHrPermissions.EventApprove]).ToArray()),
        ["line-manager"] = Build(userId: 3, employeeId: 3, scope: "department", departments: [2, 4],
        [
            CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeProfileUpdate, CoreHrPermissions.OrganizationRead,
            CoreHrPermissions.OnboardingRead, CoreHrPermissions.OnboardingManage,
            CoreHrPermissions.EventRead, CoreHrPermissions.EventWrite, CoreHrPermissions.DocumentRead
        ]),
        ["employee"] = Build(userId: 4, employeeId: 4, scope: "self", departments: [],
        [
            CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeProfileUpdate, CoreHrPermissions.OrganizationRead,
            CoreHrPermissions.OnboardingRead, CoreHrPermissions.EventRead,
            CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentUpload
        ]),
        ["it-admin"] = Build(userId: 5, employeeId: null, scope: "self", departments: [],
            [CoreHrPermissions.OnboardingRead, CoreHrPermissions.OnboardingManage])
    };

    private static Claim[] Build(long userId, long? employeeId, string scope, long[] departments, string[] permissions)
    {
        var claims = new List<Claim>
        {
            new("sub", $"dev|{userId}"),
            new("qlns_user_id", userId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("data_scope", scope)
        };
        if (employeeId.HasValue)
        {
            claims.Add(new Claim("qlns_employee_id", employeeId.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }

        claims.AddRange(departments.Select(d => new Claim("department_id", d.ToString(System.Globalization.CultureInfo.InvariantCulture))));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));
        return [.. claims];
    }
}
