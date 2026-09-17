using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

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

    private static readonly string[] AllLifecyclePermissions =
    [
        ProbationPermissions.Read, ProbationPermissions.Manage, ProbationPermissions.Decide,
        OffboardingPermissions.Read, OffboardingPermissions.Write, OffboardingPermissions.Approve
    ];

    private static readonly string[] AllContractPermissions =
    [
        ContractPermissions.Read, ContractPermissions.Write, ContractPermissions.Approve
    ];

    private static readonly string[] AllRecruitmentPermissions =
    [
        RequisitionPermissions.Read, RequisitionPermissions.Write, RequisitionPermissions.Approve, RequisitionPermissions.Publish,
        IntakePermissions.Read, IntakePermissions.Write,
        ApplicationPermissions.Read, ApplicationPermissions.Advance, ApplicationPermissions.Terminate,
        InterviewPermissions.Read, InterviewPermissions.Manage,
        EvaluationPermissions.Read, EvaluationPermissions.ReadAll, EvaluationPermissions.Submit, EvaluationPermissions.Unlock,
        OfferPermissions.Read, OfferPermissions.Write, OfferPermissions.Approve
    ];

    /// <summary>Personas match users/employees in database/seed_dev.sql and the role matrix in docs/user_stories.md §5.1.</summary>
    private static readonly Dictionary<string, Claim[]> Personas = new(StringComparer.OrdinalIgnoreCase)
    {
        // HR Manager: approves everything, organization-wide.
        ["hr-manager"] = Build(userId: 1, employeeId: 2, scope: "organization", departments: [],
            Union(AllCoreHrPermissions, AllLifecyclePermissions, AllContractPermissions, AllRecruitmentPermissions)),

        // HR Officer (C&B / Records): drafts and operates, never approves.
        ["hr-officer"] = Build(userId: 2, employeeId: 6, scope: "organization", departments: [],
            Union(
                AllCoreHrPermissions.Except([CoreHrPermissions.EventApprove]),
                [ProbationPermissions.Read, ProbationPermissions.Manage, OffboardingPermissions.Read, OffboardingPermissions.Write],
                [ContractPermissions.Read, ContractPermissions.Write],
                [RequisitionPermissions.Read, IntakePermissions.Read, ApplicationPermissions.Read, InterviewPermissions.Read, OfferPermissions.Read])),

        // Line Manager / Hiring Manager: department scope (2, 4); raises requisitions, reviews probation, confirms handover.
        ["line-manager"] = Build(userId: 3, employeeId: 3, scope: "department", departments: [2, 4],
            Union(
                [
                    CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeProfileUpdate, CoreHrPermissions.OrganizationRead,
                    CoreHrPermissions.OnboardingRead, CoreHrPermissions.OnboardingManage,
                    CoreHrPermissions.EventRead, CoreHrPermissions.EventWrite, CoreHrPermissions.DocumentRead
                ],
                [ProbationPermissions.Read, OffboardingPermissions.Read, OffboardingPermissions.Write],
                [ContractPermissions.Read],
                [
                    RequisitionPermissions.Read, RequisitionPermissions.Write, IntakePermissions.Read, ApplicationPermissions.Read,
                    InterviewPermissions.Read, EvaluationPermissions.Read, EvaluationPermissions.Submit, OfferPermissions.Read
                ])),

        // Recruiter: organization-wide recruitment operations; no employee record.
        ["recruiter"] = Build(userId: 7, employeeId: null, scope: "organization", departments: [],
            Union(
                [CoreHrPermissions.OrganizationRead],
                [
                    RequisitionPermissions.Read, RequisitionPermissions.Publish,
                    IntakePermissions.Read, IntakePermissions.Write,
                    ApplicationPermissions.Read, ApplicationPermissions.Advance, ApplicationPermissions.Terminate,
                    InterviewPermissions.Read, InterviewPermissions.Manage, EvaluationPermissions.Read,
                    OfferPermissions.Read, OfferPermissions.Write
                ])),

        // Employee: self scope only.
        ["employee"] = Build(userId: 4, employeeId: 4, scope: "self", departments: [],
            Union(
                [
                    CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeProfileUpdate, CoreHrPermissions.OrganizationRead,
                    CoreHrPermissions.OnboardingRead, CoreHrPermissions.EventRead,
                    CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentUpload
                ],
                [ProbationPermissions.Read, OffboardingPermissions.Read],
                [ContractPermissions.Read])),

        // IT Admin: onboarding/offboarding checklist assignee.
        ["it-admin"] = Build(userId: 5, employeeId: null, scope: "self", departments: [],
            Union(
                [CoreHrPermissions.OnboardingRead, CoreHrPermissions.OnboardingManage],
                [OffboardingPermissions.Read, OffboardingPermissions.Write]))
    };

    private static string[] Union(params IEnumerable<string>[] groups) =>
        groups.SelectMany(group => group).Distinct(StringComparer.Ordinal).ToArray();

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
