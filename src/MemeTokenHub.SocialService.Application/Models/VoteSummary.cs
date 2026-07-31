using MemeTokenHub.SocialService.Domain.Enums;
namespace MemeTokenHub.SocialService.Application.Models; public sealed class VoteSummary { public long Hot { get; init; } public long NotHot { get; init; } public VoteValue? ViewerVote { get; init; } }
