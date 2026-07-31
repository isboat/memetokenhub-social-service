using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoIndexInitializerHostedService(
    IMongoIndexInitializer indexInitializer,
    ILogger<MongoIndexInitializerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await indexInitializer.CreateIndexesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(
                exception,
                "MongoDB indexes could not be initialized. Readiness will remain unhealthy until MongoDB is available.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
