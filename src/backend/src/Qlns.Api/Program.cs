using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
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
builder.Services.AddScoped<RecruitmentPipelineService>();
builder.Services.AddDataAccess(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Authentication:Authority"];
        options.Audience = builder.Configuration["Authentication:Audience"];
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RecruitmentRead", policy =>
        policy.RequireClaim("permission", "recruitment.application.read"));
    options.AddPolicy("RecruitmentAdvance", policy =>
        policy.RequireClaim("permission", "recruitment.application.advance"));
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseCors("WebClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapOpenApi();
app.MapControllers();

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
