namespace MemeTokenHub.SocialService.Domain.Entities;

public sealed class TokenSupport { public string Id { get; init; } = Guid.NewGuid().ToString(); public required string KolUserId { get; init; } public required string TokenId { get; init; } public string? Statement { get; init; } public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow; public DateTimeOffset? WithdrawnAt { get; set; } }
