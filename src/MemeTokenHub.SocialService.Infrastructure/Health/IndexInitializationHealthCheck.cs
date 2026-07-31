using MemeTokenHub.SocialService.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.SocialService.Infrastructure.Health;

public sealed class IndexInitializationHealthCheck(IndexInitializationState state) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        HealthCheckResult result = state.IsInitialized
            ? HealthCheckResult.Healthy("MongoDB indexes are initialized.")
            : HealthCheckResult.Unhealthy("MongoDB indexes are not initialized.");
        return Task.FromResult(result);
    }
}
