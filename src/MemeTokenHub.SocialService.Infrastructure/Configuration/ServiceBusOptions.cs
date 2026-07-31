namespace MemeTokenHub.SocialService.Infrastructure.Configuration;

public sealed class ServiceBusOptions
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string TopicName { get; init; } = "social-events";
}
