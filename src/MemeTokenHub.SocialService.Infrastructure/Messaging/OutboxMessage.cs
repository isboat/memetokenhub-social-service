using System.Text.Json;

namespace MemeTokenHub.SocialService.Infrastructure.Messaging;

public sealed class OutboxMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();

    public required string EventType { get; init; }

    public int SchemaVersion { get; init; } = 1;

    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    public string CorrelationId { get; init; } = Guid.NewGuid().ToString();

    public required string Producer { get; init; }

    public required string SubjectId { get; init; }

    public required JsonElement Payload { get; init; }

    public DateTimeOffset? PublishedAt { get; set; }

    public int DeliveryAttempts { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset? DeadLetteredAt { get; set; }

    public string? DeadLetterReason { get; set; }
}
