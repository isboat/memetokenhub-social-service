using MemeTokenHub.SocialService.Domain.Enums;
namespace MemeTokenHub.SocialService.Domain.Entities;

public sealed class SocialPost { public string Id { get; init; } = Guid.NewGuid().ToString(); public required string AuthorId { get; init; } public string? TokenId { get; init; } public required string Content { get; set; } public IReadOnlyCollection<string> MediaUrls { get; init; } = []; public PostAccess Access { get; init; } public ModerationStatus ModerationStatus { get; set; } = ModerationStatus.Published; public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow; public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow; }
