using Microsoft.Extensions.Hosting;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoIndexInitializerHostedService(IMongoIndexInitializer indexInitializer) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => indexInitializer.CreateIndexesAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
