using MemeTokenHub.SocialService.Domain.Enums;

namespace MemeTokenHub.SocialService.Application.Models;

public sealed class DeactivatedSocialAction
{
    public required string UserId { get; init; }
    public string? TokenId { get; init; }
    public FollowTargetType? TargetType { get; init; }
    public string? TargetId { get; init; }
    public bool IsActive { get; init; }
}
