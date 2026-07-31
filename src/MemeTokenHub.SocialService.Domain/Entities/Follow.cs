using MemeTokenHub.SocialService.Domain.Enums;
namespace MemeTokenHub.SocialService.Domain.Entities;

public sealed class Follow { public string Id { get; init; } = Guid.NewGuid().ToString(); public required string FollowerId { get; init; } public required FollowTargetType TargetType { get; init; } public required string TargetId { get; init; } public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow; }
