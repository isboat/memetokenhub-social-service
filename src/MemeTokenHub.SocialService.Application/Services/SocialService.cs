using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Entities;
using MemeTokenHub.SocialService.Domain.Enums;

namespace MemeTokenHub.SocialService.Application.Services;

/// <summary>Coordinates social domain actions and integration events.</summary>
public sealed class SocialService(ISocialRepository repository, IEventPublisher eventPublisher, IUnitOfWork unitOfWork) : ISocialService
{
    /// <summary>Creates or restores the current user's follow relationship.</summary>
    public async Task<Follow> FollowAsync(string userId, CreateFollowRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetType == FollowTargetType.User && userId == request.TargetId)
        {
            throw new InvalidOperationException("A user cannot follow themselves.");
        }

        return await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            Follow follow = new() { FollowerId = userId, TargetType = request.TargetType, TargetId = request.TargetId };
            Follow savedFollow = await repository.UpsertFollowAsync(follow, transactionCancellationToken);
            await eventPublisher.PublishAsync("FollowChanged", savedFollow.Id, savedFollow, transactionCancellationToken);
            return savedFollow;
        }, cancellationToken);
    }

    /// <summary>Removes a follow relationship owned by the current user.</summary>
    public async Task UnfollowAsync(string userId, FollowTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            bool removed = await repository.DeleteFollowAsync(userId, targetType, targetId, transactionCancellationToken);
            if (!removed)
                throw new KeyNotFoundException("Follow relationship was not found.");
            await eventPublisher.PublishAsync("FollowChanged", targetId, new DeactivatedSocialAction { UserId = userId, TargetType = targetType, TargetId = targetId, IsActive = false }, transactionCancellationToken);
        }, cancellationToken);
    }

    /// <summary>Returns tracked targets for a user.</summary>
    public Task<PagedResult<Follow>> GetFollowsAsync(string userId, FollowTargetType? targetType, int limit, int offset, CancellationToken cancellationToken) => repository.GetFollowsAsync(userId, targetType, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Records an idempotent token like for the current user.</summary>
    public Task<Engagement> LikeAsync(string userId, string tokenId, CancellationToken cancellationToken) => AddEngagementAsync(userId, tokenId, "Like", null, cancellationToken);

    /// <summary>Records a sanitized token comment for the current user.</summary>
    public Task<Engagement> CommentAsync(string userId, string tokenId, CreateCommentRequest request, CancellationToken cancellationToken) => AddEngagementAsync(userId, tokenId, "Comment", NormalizeRequiredContent(request.Content, "Comment content is required."), cancellationToken);

    /// <summary>Returns token likes and comments.</summary>
    public Task<PagedResult<Engagement>> GetEngagementAsync(string tokenId, int limit, int offset, CancellationToken cancellationToken) => repository.GetEngagementAsync(tokenId, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Casts or replaces the current user's token sentiment vote.</summary>
    public async Task<TokenVote> VoteAsync(string userId, string tokenId, CreateVoteRequest request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            TokenVote vote = new() { UserId = userId, TokenId = tokenId, Value = request.Value };
            TokenVote savedVote = await repository.UpsertVoteAsync(vote, transactionCancellationToken);
            await eventPublisher.PublishAsync("TokenVoteChanged", tokenId, savedVote, transactionCancellationToken);
            return savedVote;
        }, cancellationToken);
    }

    /// <summary>Removes the current user's active token vote.</summary>
    public async Task RemoveVoteAsync(string userId, string tokenId, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            if (!await repository.DeleteVoteAsync(userId, tokenId, transactionCancellationToken))
                throw new KeyNotFoundException("Vote was not found.");
            await eventPublisher.PublishAsync("TokenVoteChanged", tokenId, new DeactivatedSocialAction { UserId = userId, TokenId = tokenId, IsActive = false }, transactionCancellationToken);
        }, cancellationToken);
    }

    /// <summary>Returns aggregate sentiment and optionally the viewer's current vote.</summary>
    public Task<VoteSummary> GetVoteAsync(string tokenId, string? viewerId, CancellationToken cancellationToken) => repository.GetVoteSummaryAsync(tokenId, viewerId, cancellationToken);

    /// <summary>Creates or restores a timestamped KOL endorsement.</summary>
    public async Task<TokenSupport> SupportAsync(string userId, string tokenId, CreateSupportRequest request, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            TokenSupport support = new() { KolUserId = userId, TokenId = tokenId, Statement = request.Statement?.Trim() };
            TokenSupport savedSupport = await repository.UpsertSupportAsync(support, transactionCancellationToken);
            await eventPublisher.PublishAsync("KolSupportChanged", tokenId, savedSupport, transactionCancellationToken);
            return savedSupport;
        }, cancellationToken);
    }

    /// <summary>Withdraws support while preserving its original timestamp.</summary>
    public async Task WithdrawSupportAsync(string userId, string tokenId, CancellationToken cancellationToken)
    {
        await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            if (!await repository.WithdrawSupportAsync(userId, tokenId, transactionCancellationToken))
                throw new KeyNotFoundException("Support was not found.");
            await eventPublisher.PublishAsync("KolSupportChanged", tokenId, new DeactivatedSocialAction { UserId = userId, TokenId = tokenId, IsActive = false }, transactionCancellationToken);
        }, cancellationToken);
    }

    /// <summary>Returns active or historical token supporters.</summary>
    public Task<PagedResult<TokenSupport>> GetSupportersAsync(string tokenId, bool includeWithdrawn, int limit, int offset, CancellationToken cancellationToken) => repository.GetSupportersAsync(tokenId, includeWithdrawn, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Publishes a community post authored by the current user.</summary>
    public async Task<SocialPost> CreatePostAsync(string userId, CreatePostRequest request, CancellationToken cancellationToken)
    {
        string content = NormalizeRequiredContent(request.Content, "Post content is required.");
        return await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            SocialPost post = new() { AuthorId = userId, TokenId = request.TokenId, Content = content, MediaUrls = request.MediaUrls, Access = request.Access };
            SocialPost savedPost = await repository.AddPostAsync(post, transactionCancellationToken);
            await eventPublisher.PublishAsync("PostPublished", savedPost.Id, savedPost, transactionCancellationToken);
            return savedPost;
        }, cancellationToken);
    }

    /// <summary>Returns a post by identifier when it exists.</summary>
    public Task<SocialPost?> GetPostAsync(string postId, CancellationToken cancellationToken) => repository.GetPostAsync(postId, cancellationToken);

    /// <summary>Returns a filtered page of public community posts.</summary>
    public Task<PagedResult<SocialPost>> GetPostsAsync(string? authorId, string? tokenId, int limit, int offset, CancellationToken cancellationToken) => repository.GetPostsAsync(authorId, tokenId, ClampLimit(limit), Math.Max(0, offset), cancellationToken);

    /// <summary>Returns the user's calculated reputation record.</summary>
    public Task<Reputation?> GetReputationAsync(string userId, CancellationToken cancellationToken) => repository.GetReputationAsync(userId, cancellationToken);

    private async Task<Engagement> AddEngagementAsync(string userId, string tokenId, string type, string? content, CancellationToken cancellationToken)
    {
        return await unitOfWork.ExecuteAsync(async transactionCancellationToken =>
        {
            Engagement engagement = new() { UserId = userId, TokenId = tokenId, Type = type, Content = content };
            Engagement savedEngagement = await repository.AddEngagementAsync(engagement, transactionCancellationToken);
            await eventPublisher.PublishAsync("EngagementChanged", tokenId, savedEngagement, transactionCancellationToken);
            return savedEngagement;
        }, cancellationToken);
    }

    private static string NormalizeRequiredContent(string content, string errorMessage)
    {
        string normalizedContent = content.Trim();
        return normalizedContent.Length > 0 ? normalizedContent : throw new InvalidOperationException(errorMessage);
    }

    private static int ClampLimit(int limit) => Math.Clamp(limit, 1, 100);
}
