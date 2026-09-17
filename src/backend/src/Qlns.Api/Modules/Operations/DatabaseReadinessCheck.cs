using Microsoft.Extensions.Diagnostics.HealthChecks;
using Qlns.DataAccess;

namespace Qlns.Api.Modules.Operations;

/// <summary>
/// Readiness dependency check: the service is ready when PostgreSQL accepts a connection.
/// The probe response never carries the exception or connection details (see <see cref="HealthProbes"/>).
/// </summary>
public sealed class DatabaseReadinessCheck(QlnsDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Database is not reachable.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("Database check failed.", exception);
        }
    }
}
