using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Qlns.Api.Development;
using Qlns.Api.Modules.Contracts.Shared;
using Qlns.Api.Modules.CoreHr.Offboarding;
using Qlns.Api.Modules.CoreHr.Probation;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.Api.Modules.Operations;
using Qlns.Api.Modules.Recruitment.Applications;
using Qlns.Api.Modules.Recruitment.Evaluations;
using Qlns.Api.Modules.Recruitment.Intake;
using Qlns.Api.Modules.Recruitment.Interviews;
using Qlns.Api.Modules.Recruitment.Offers;
using Qlns.Api.Modules.Recruitment.Requisitions;
using Qlns.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Request syntax or binding failed",
            Type = "https://qlns.example/problems/invalid-request",
            Instance = context.HttpContext.Request.Path
        };
        problem.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem);
    };
});
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddPolicy("WebClient", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("ETag"));
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataAccess(builder.Configuration);
builder.Services.AddHealthProbes();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        if (DevelopmentAuthentication.IsEnabled(builder.Environment, builder.Configuration))
        {
            // Local development without an Identity Provider: symmetric key + GET /dev/token.
            DevelopmentAuthentication.ConfigureJwtBearer(options, builder.Configuration);
        }
        else
        {
            options.Authority = builder.Configuration["Authentication:Authority"];
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        }

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(CreateSecurityProblem(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    "Authentication required"),
                    cancellationToken: context.HttpContext.RequestAborted);
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(CreateSecurityProblem(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    "Access forbidden"),
                    cancellationToken: context.HttpContext.RequestAborted);
            }
        };
    });

// Endpoint-level policies require the coarse permission claim; each business service re-checks the
// finer action-level permission and the actor's data scope (deny by default, quality goal Q1).
builder.Services.AddAuthorization(options =>
{
    options.AddCoreHrPolicies();
    options.AddProbationPolicies();
    options.AddOffboardingPolicies();
    options.AddContractPolicies();
    options.AddRequisitionPolicies();
    options.AddIntakePolicies();
    options.AddApplicationPolicies();
    options.AddInterviewPolicies();
    options.AddEvaluationPolicies();
    options.AddOfferPolicies();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();
app.MapHealthProbes();
if (DevelopmentAuthentication.IsEnabled(app.Environment, app.Configuration))
{
    app.MapDevelopmentTokenEndpoint();
}

app.Run();

static ProblemDetails CreateSecurityProblem(HttpContext context, int status, string title)
{
    var problem = new ProblemDetails
    {
        Status = status,
        Title = title,
        Type = status == 401
            ? "https://qlns.example/problems/authentication-required"
            : "https://qlns.example/problems/access-forbidden",
        Instance = context.Request.Path
    };
    problem.Extensions["correlationId"] = context.TraceIdentifier;
    return problem;
}

public partial class Program;
