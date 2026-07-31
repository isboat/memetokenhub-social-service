using MemeTokenHub.SocialService.Domain.Enums;
namespace MemeTokenHub.SocialService.Domain.Entities;

public sealed class TokenVote { public string Id { get; init; } = Guid.NewGuid().ToString(); public required string UserId { get; init; } public required string TokenId { get; init; } public VoteValue Value { get; set; } public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow; public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow; }
