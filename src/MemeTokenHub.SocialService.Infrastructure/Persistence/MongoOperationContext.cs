using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoOperationContext : IMongoOperationContext
{
    public IClientSessionHandle? Session { get; set; }
}
