namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public interface IMongoIndexInitializer
{
    Task CreateIndexesAsync(CancellationToken cancellationToken);
}
