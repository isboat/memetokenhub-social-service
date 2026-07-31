using System.Text.Json;
using MemeTokenHub.SocialService.Application.Interfaces;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Messaging;

public sealed class MongoEventOutbox(IMongoDatabase database) : IEventPublisher
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

        return messages.InsertOneAsync(message, cancellationToken: cancellationToken);
    }
}
