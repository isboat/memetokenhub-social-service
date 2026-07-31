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
            OutboxMessage? message = await messages
                .Find(item => item.PublishedAt == null)
                .SortBy(item => item.OccurredAt)
                .FirstOrDefaultAsync(stoppingToken);

            if (message is null)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                continue;
            }

            await DeliverAsync(sender, message, stoppingToken);
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
            logger.LogError(exception, "Failed to deliver outbox event {EventId}", outboxMessage.Id);
            await messages.UpdateOneAsync(
                item => item.Id == outboxMessage.Id,
                Builders<OutboxMessage>.Update
                    .Inc(item => item.DeliveryAttempts, 1)
                    .Set(item => item.LastError, exception.Message),
                cancellationToken: cancellationToken);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
    }
}
