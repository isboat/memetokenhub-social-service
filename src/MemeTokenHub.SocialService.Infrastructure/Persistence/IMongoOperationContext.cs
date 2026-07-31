using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public interface IMongoOperationContext
{
    IClientSessionHandle? Session { get; set; }
}
