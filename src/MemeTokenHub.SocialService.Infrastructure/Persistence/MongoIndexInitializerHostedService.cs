using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoIndexInitializerHostedService(
    IMongoIndexInitializer indexInitializer,
    IndexInitializationState state,
    ILogger<MongoIndexInitializerHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !state.IsInitialized)
        {
            try
            {
                await indexInitializer.CreateIndexesAsync(stoppingToken);
                state.IsInitialized = true;
                state.LastError = null;
                logger.LogInformation("MongoDB indexes are initialized.");
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                state.LastError = exception.Message;
                logger.LogError(exception, "MongoDB index initialization failed and will be retried.");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
