using System.Text.Json;
using Azure.Messaging.ServiceBus;
using MemeTokenHub.SocialService.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Messaging;

public sealed class OutboxDeliveryWorker(
    IMongoDatabase database,
    ServiceBusOptions options,
    ILogger<OutboxDeliveryWorker> logger) : BackgroundService
{
    private const int MaximumDeliveryAttempts = 5;

    private readonly IMongoCollection<OutboxMessage> messages = database.GetCollection<OutboxMessage>("EventOutbox");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            logger.LogWarning("Service Bus is not configured; integration events will remain in the outbox.");
            return;
        }

        await using ServiceBusClient client = new(options.ConnectionString);
        await using ServiceBusSender sender = client.CreateSender(options.TopicName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                OutboxMessage? message = await messages
                    .Find(item => item.PublishedAt == null && item.DeadLetteredAt == null)
                    .SortBy(item => item.OccurredAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (message is not null)
                {
                    await DeliverAsync(sender, message, stoppingToken);
                    continue;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "The event outbox could not be queried.");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }

    private async Task DeliverAsync(ServiceBusSender sender, OutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        try
        {
            string body = JsonSerializer.Serialize(outboxMessage);
            ServiceBusMessage message = new(body)
            {
                MessageId = outboxMessage.Id,
                Subject = outboxMessage.EventType,
                ContentType = "application/json",
                CorrelationId = outboxMessage.CorrelationId,
            };
            message.ApplicationProperties["schemaVersion"] = outboxMessage.SchemaVersion;
            message.ApplicationProperties["producer"] = outboxMessage.Producer;

            await sender.SendMessageAsync(message, cancellationToken);
            await messages.UpdateOneAsync(
                item => item.Id == outboxMessage.Id && item.PublishedAt == null,
                Builders<OutboxMessage>.Update
                    .Set(item => item.PublishedAt, DateTimeOffset.UtcNow)
                    .Inc(item => item.DeliveryAttempts, 1)
                    .Unset(item => item.LastError),
                cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            int attemptNumber = outboxMessage.DeliveryAttempts + 1;
            bool shouldDeadLetter = attemptNumber >= MaximumDeliveryAttempts;
            logger.LogError(
                exception,
                "Failed to deliver outbox event {EventId} on attempt {AttemptNumber}; dead-letter: {ShouldDeadLetter}",
                outboxMessage.Id,
                attemptNumber,
                shouldDeadLetter);

            UpdateDefinition<OutboxMessage> update = Builders<OutboxMessage>.Update
                .Set(item => item.DeliveryAttempts, attemptNumber)
                .Set(item => item.LastError, exception.Message);
            if (shouldDeadLetter)
            {
                update = update
                    .Set(item => item.DeadLetteredAt, DateTimeOffset.UtcNow)
                    .Set(item => item.DeadLetterReason, "Maximum delivery attempts exceeded.");
            }

            await messages.UpdateOneAsync(
                item => item.Id == outboxMessage.Id,
                update,
                cancellationToken: cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
