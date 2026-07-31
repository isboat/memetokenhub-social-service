using Azure.Messaging.ServiceBus.Administration;
using MemeTokenHub.SocialService.Infrastructure.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.SocialService.Infrastructure.Health;

public sealed class ServiceBusHealthCheck(ServiceBusOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return HealthCheckResult.Unhealthy("Azure Service Bus is not configured.");
        }

        try
        {
            ServiceBusAdministrationClient client = new(options.ConnectionString);
            await client.GetTopicRuntimePropertiesAsync(options.TopicName, cancellationToken);
            return HealthCheckResult.Healthy("Azure Service Bus topic is available.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                "Azure Service Bus topic is unavailable.",
                exception);
        }
    }
}
