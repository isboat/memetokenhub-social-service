namespace MemeTokenHub.SocialService.Domain.Entities;

public sealed class Reputation { public required string UserId { get; init; } public int Score { get; set; } public IReadOnlyCollection<string> Badges { get; set; } = []; public int ClaimsApproved { get; set; } public int TokensPublished { get; set; } public int FollowersCount { get; set; } public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow; }
