using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Entities;
using MemeTokenHub.SocialService.Domain.Enums;

namespace MemeTokenHub.SocialService.Application.Services;

/// <summary>Coordinates social domain actions and integration events.</summary>
public sealed class SocialService(ISocialRepository repository, IEventPublisher eventPublisher) : ISocialService
{
    /// <summary>Creates or restores the current user's follow relationship.</summary>
    public async Task<Follow> FollowAsync(string userId, CreateFollowRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetType == FollowTargetType.User && userId == request.TargetId)
        {
            throw new InvalidOperationException("A user cannot follow themselves.");
        }

        Follow follow = new() { FollowerId = userId, TargetType = request.TargetType, TargetId = request.TargetId };
        Follow savedFollow = await repository.UpsertFollowAsync(follow, cancellationToken);
        await eventPublisher.PublishAsync("FollowChanged", savedFollow.Id, savedFollow, cancellationToken);
        return savedFollow;
    }

    /// <summary>Removes a follow relationship owned by the current user.</summary>
    public async Task UnfollowAsync(string userId, FollowTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        bool removed = await repository.DeleteFollowAsync(userId, targetType, targetId, cancellationToken);
        if (!removed)
            throw new KeyNotFoundException("Follow relationship was not found.");
        await eventPublisher.PublishAsync("FollowChanged", targetId, new DeactivatedSocialAction { UserId = userId, TargetType = targetType, TargetId = targetId, IsActive = false }, cancellationToken);
    }

    /// <summary>Returns tracked targets for a user.</summary>
    public Task<PagedResult<Follow>> GetFollowsAsync(string userId, FollowTargetType? targetType, int limit, int offset, CancellationToken cancellationToken) => repository.GetFollowsAsync(userId, targetType, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Records an idempotent token like for the current user.</summary>
    public Task<Engagement> LikeAsync(string userId, string tokenId, CancellationToken cancellationToken) => AddEngagementAsync(userId, tokenId, "Like", null, cancellationToken);

    /// <summary>Records a sanitized token comment for the current user.</summary>
    public Task<Engagement> CommentAsync(string userId, string tokenId, CreateCommentRequest request, CancellationToken cancellationToken) => AddEngagementAsync(userId, tokenId, "Comment", request.Content.Trim(), cancellationToken);

    /// <summary>Returns token likes and comments.</summary>
    public Task<PagedResult<Engagement>> GetEngagementAsync(string tokenId, int limit, int offset, CancellationToken cancellationToken) => repository.GetEngagementAsync(tokenId, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Casts or replaces the current user's token sentiment vote.</summary>
    public async Task<TokenVote> VoteAsync(string userId, string tokenId, CreateVoteRequest request, CancellationToken cancellationToken)
    {
        TokenVote vote = new() { UserId = userId, TokenId = tokenId, Value = request.Value };
        TokenVote savedVote = await repository.UpsertVoteAsync(vote, cancellationToken);
        await eventPublisher.PublishAsync("TokenVoteChanged", tokenId, savedVote, cancellationToken);
        return savedVote;
    }

    /// <summary>Removes the current user's active token vote.</summary>
    public async Task RemoveVoteAsync(string userId, string tokenId, CancellationToken cancellationToken)
    {
        if (!await repository.DeleteVoteAsync(userId, tokenId, cancellationToken))
            throw new KeyNotFoundException("Vote was not found.");
        await eventPublisher.PublishAsync("TokenVoteChanged", tokenId, new DeactivatedSocialAction { UserId = userId, TokenId = tokenId, IsActive = false }, cancellationToken);
    }

    /// <summary>Returns aggregate sentiment and optionally the viewer's current vote.</summary>
    public Task<VoteSummary> GetVoteAsync(string tokenId, string? viewerId, CancellationToken cancellationToken) => repository.GetVoteSummaryAsync(tokenId, viewerId, cancellationToken);

    /// <summary>Creates or restores a timestamped KOL endorsement.</summary>
    public async Task<TokenSupport> SupportAsync(string userId, string tokenId, CreateSupportRequest request, CancellationToken cancellationToken)
    {
        TokenSupport support = new() { KolUserId = userId, TokenId = tokenId, Statement = request.Statement?.Trim() };
        TokenSupport savedSupport = await repository.UpsertSupportAsync(support, cancellationToken);
        await eventPublisher.PublishAsync("KolSupportChanged", tokenId, savedSupport, cancellationToken);
        return savedSupport;
    }

    /// <summary>Withdraws support while preserving its original timestamp.</summary>
    public async Task WithdrawSupportAsync(string userId, string tokenId, CancellationToken cancellationToken)
    {
        if (!await repository.WithdrawSupportAsync(userId, tokenId, cancellationToken))
            throw new KeyNotFoundException("Support was not found.");
        await eventPublisher.PublishAsync("KolSupportChanged", tokenId, new DeactivatedSocialAction { UserId = userId, TokenId = tokenId, IsActive = false }, cancellationToken);
    }

    /// <summary>Returns active or historical token supporters.</summary>
    public Task<PagedResult<TokenSupport>> GetSupportersAsync(string tokenId, bool includeWithdrawn, int limit, int offset, CancellationToken cancellationToken) => repository.GetSupportersAsync(tokenId, includeWithdrawn, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Publishes a community post authored by the current user.</summary>
    public async Task<SocialPost> CreatePostAsync(string userId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        SocialPost post = new() { AuthorId = userId, TokenId = request.TokenId, Content = request.Content.Trim(), MediaUrls = request.MediaUrls, Access = request.Access };
        SocialPost savedPost = await repository.AddPostAsync(post, cancellationToken);
        await eventPublisher.PublishAsync("PostPublished", savedPost.Id, savedPost, cancellationToken);
        return savedPost;
    }

    /// <summary>Returns a post by identifier when it exists.</summary>
    public Task<SocialPost?> GetPostAsync(string postId, CancellationToken cancellationToken) => repository.GetPostAsync(postId, cancellationToken);

    /// <summary>Returns a filtered page of public community posts.</summary>
    public Task<PagedResult<SocialPost>> GetPostsAsync(string? authorId, string? tokenId, int limit, int offset, CancellationToken cancellationToken) => repository.GetPostsAsync(authorId, tokenId, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Returns the user's calculated reputation record.</summary>
    public Task<Reputation?> GetReputationAsync(string userId, CancellationToken cancellationToken) => repository.GetReputationAsync(userId, cancellationToken);

    private async Task<Engagement> AddEngagementAsync(string userId, string tokenId, string type, string? content, CancellationToken cancellationToken)
    {
        Engagement engagement = new() { UserId = userId, TokenId = tokenId, Type = type, Content = content };
        Engagement savedEngagement = await repository.AddEngagementAsync(engagement, cancellationToken);
        await eventPublisher.PublishAsync("EngagementChanged", tokenId, savedEngagement, cancellationToken);
        return savedEngagement;
    }

    private static int ClampLimit(int limit) => Math.Clamp(limit, 1, 100);
}
