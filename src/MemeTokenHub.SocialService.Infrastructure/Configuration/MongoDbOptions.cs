namespace MemeTokenHub.SocialService.Infrastructure.Configuration;

public sealed class MongoDbOptions
{
    public const string SectionName = "MongoDb";
    public required string ConnectionString { get; init; }
    public string DatabaseName { get; init; } = "MemeTokenHubSocial";
}
