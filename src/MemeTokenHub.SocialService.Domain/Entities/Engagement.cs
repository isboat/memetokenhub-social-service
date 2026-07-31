namespace MemeTokenHub.SocialService.Domain.Entities;

public sealed class Engagement { public string Id { get; init; } = Guid.NewGuid().ToString(); public required string UserId { get; init; } public required string TokenId { get; init; } public required string Type { get; init; } public string? Content { get; init; } public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow; }
