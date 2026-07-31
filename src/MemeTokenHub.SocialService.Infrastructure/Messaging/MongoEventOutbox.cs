using System.Text.Json;
using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Infrastructure.Persistence;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Messaging;

public sealed class MongoEventOutbox(IMongoDatabase database, IMongoOperationContext operationContext) : IEventPublisher
{
    private readonly IMongoCollection<OutboxMessage> messages = database.GetCollection<OutboxMessage>("EventOutbox");

    public Task PublishAsync<T>(string eventType, string subjectId, T payload, CancellationToken cancellationToken)
    {
        OutboxMessage message = new()
        {
            EventType = eventType,
            Producer = "social-service",
            SubjectId = subjectId,
            Payload = JsonSerializer.SerializeToElement(payload),
        };

        return messages.InsertOneAsync(operationContext.Session!, message, cancellationToken: cancellationToken);
    }
}
