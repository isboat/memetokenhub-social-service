using MemeTokenHub.SocialService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace MemeTokenHub.SocialService.Infrastructure.Messaging;

public sealed class LoggingEventPublisher(ILogger<LoggingEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync<T>(string eventType, string subjectId, T payload, CancellationToken cancellationToken)
    {
        logger.LogInformation("Integration event {EventType} prepared for {SubjectId}", eventType, subjectId);
        return Task.CompletedTask;
    }
}
