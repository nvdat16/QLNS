using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Qlns.Api.Modules.Operations;

/// <summary>
/// Operations tag of the OpenAPI contract: <c>GET /health/live</c> and <c>GET /health/ready</c>.
/// Both are anonymous platform probes for the deployment view (docs/architecture.md §7), not business
/// functions. The body is the minimal <c>HealthStatus</c> schema — no dependency names, no secrets, no details.
/// </summary>
public static class HealthProbes
{
    public const string LivePath = "/health/live";
    public const string ReadyPath = "/health/ready";
    private const string ReadinessTag = "ready";

    public static IServiceCollection AddHealthProbes(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<DatabaseReadinessCheck>("database", tags: [ReadinessTag]);
        return services;
    }

    public static void MapHealthProbes(this WebApplication app)
    {
        // Liveness runs no checks: the process answering is the signal.
        app.MapHealthChecks(LivePath, new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteStatusAsync
        }).AllowAnonymous().ExcludeFromDescription();

        app.MapHealthChecks(ReadyPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadinessTag),
            ResponseWriter = WriteStatusAsync,
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).AllowAnonymous().ExcludeFromDescription();
    }

    private static Task WriteStatusAsync(HttpContext context, HealthReport report)
    {
        var timeProvider = context.RequestServices.GetRequiredService<TimeProvider>();
        var payload = new HealthStatusResponse(
            report.Status == HealthStatus.Unhealthy ? "unhealthy" : "healthy",
            timeProvider.GetUtcNow());

        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(
            JsonSerializer.Serialize(payload, JsonSerializerOptions.Web),
            context.RequestAborted);
    }
}

/// <summary>OpenAPI <c>HealthStatus</c>.</summary>
public sealed record HealthStatusResponse(string Status, DateTimeOffset Timestamp);
