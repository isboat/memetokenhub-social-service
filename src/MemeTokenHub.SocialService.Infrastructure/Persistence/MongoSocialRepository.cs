using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Entities;
using MemeTokenHub.SocialService.Domain.Enums;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoSocialRepository(IMongoDatabase database, IMongoOperationContext operationContext) : ISocialRepository
{
    private readonly IMongoCollection<Follow> follows = database.GetCollection<Follow>("Follows");
    private readonly IMongoCollection<Engagement> engagements = database.GetCollection<Engagement>("Engagements");
    private readonly IMongoCollection<TokenVote> votes = database.GetCollection<TokenVote>("Votes");
    private readonly IMongoCollection<TokenSupport> supports = database.GetCollection<TokenSupport>("TokenSupports");
    private readonly IMongoCollection<SocialPost> posts = database.GetCollection<SocialPost>("Posts");
    private readonly IMongoCollection<Reputation> reputations = database.GetCollection<Reputation>("Reputations");

    public async Task<Follow> UpsertFollowAsync(Follow follow, CancellationToken cancellationToken)
    {
        FilterDefinition<Follow> filter = Builders<Follow>.Filter.Where(item => item.FollowerId == follow.FollowerId && item.TargetType == follow.TargetType && item.TargetId == follow.TargetId);
        UpdateDefinition<Follow> update = Builders<Follow>.Update
            .SetOnInsert(item => item.Id, follow.Id)
            .SetOnInsert(item => item.FollowerId, follow.FollowerId)
            .SetOnInsert(item => item.TargetType, follow.TargetType)
            .SetOnInsert(item => item.TargetId, follow.TargetId)
            .SetOnInsert(item => item.CreatedAt, follow.CreatedAt);
        return await follows.FindOneAndUpdateAsync(operationContext.Session!, filter, update, new FindOneAndUpdateOptions<Follow> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<bool> DeleteFollowAsync(string followerId, FollowTargetType targetType, string targetId, CancellationToken cancellationToken) => (await follows.DeleteOneAsync(operationContext.Session!, item => item.FollowerId == followerId && item.TargetType == targetType && item.TargetId == targetId, cancellationToken: cancellationToken)).DeletedCount > 0;

    public async Task<PagedResult<Follow>> GetFollowsAsync(string followerId, FollowTargetType? targetType, int limit, int offset, CancellationToken cancellationToken)
    {
        FilterDefinition<Follow> filter = Builders<Follow>.Filter.Eq(item => item.FollowerId, followerId);
        if (targetType.HasValue)
            filter &= Builders<Follow>.Filter.Eq(item => item.TargetType, targetType.Value);
        return await PageAsync(follows, filter, limit, offset, cancellationToken);
    }

    public async Task<Engagement> AddEngagementAsync(Engagement engagement, CancellationToken cancellationToken)
    {
        if (engagement.Type != "Like")
        {
            await engagements.InsertOneAsync(operationContext.Session!, engagement, cancellationToken: cancellationToken);
            return engagement;
        }

        FilterDefinition<Engagement> filter = Builders<Engagement>.Filter.Where(item =>
            item.UserId == engagement.UserId && item.TokenId == engagement.TokenId && item.Type == "Like");
        UpdateDefinition<Engagement> update = Builders<Engagement>.Update
            .SetOnInsert(item => item.Id, engagement.Id)
            .SetOnInsert(item => item.UserId, engagement.UserId)
            .SetOnInsert(item => item.TokenId, engagement.TokenId)
            .SetOnInsert(item => item.Type, engagement.Type)
            .SetOnInsert(item => item.CreatedAt, engagement.CreatedAt);
        return await engagements.FindOneAndUpdateAsync(
            operationContext.Session!,
            filter,
            update,
            new FindOneAndUpdateOptions<Engagement> { IsUpsert = true, ReturnDocument = ReturnDocument.After },
            cancellationToken);
    }

    public Task<PagedResult<Engagement>> GetEngagementAsync(string tokenId, int limit, int offset, CancellationToken cancellationToken) => PageAsync(engagements, Builders<Engagement>.Filter.Eq(item => item.TokenId, tokenId), limit, offset, cancellationToken);

    public async Task<TokenVote> UpsertVoteAsync(TokenVote vote, CancellationToken cancellationToken)
    {
        FilterDefinition<TokenVote> filter = Builders<TokenVote>.Filter.Where(item => item.UserId == vote.UserId && item.TokenId == vote.TokenId);
        UpdateDefinition<TokenVote> update = Builders<TokenVote>.Update
            .Set(item => item.Value, vote.Value)
            .Set(item => item.UpdatedAt, DateTimeOffset.UtcNow)
            .SetOnInsert(item => item.Id, vote.Id)
            .SetOnInsert(item => item.UserId, vote.UserId)
            .SetOnInsert(item => item.TokenId, vote.TokenId)
            .SetOnInsert(item => item.CreatedAt, vote.CreatedAt);
        return await votes.FindOneAndUpdateAsync(operationContext.Session!, filter, update, new FindOneAndUpdateOptions<TokenVote> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<bool> DeleteVoteAsync(string userId, string tokenId, CancellationToken cancellationToken) => (await votes.DeleteOneAsync(operationContext.Session!, item => item.UserId == userId && item.TokenId == tokenId, cancellationToken: cancellationToken)).DeletedCount > 0;

    public async Task<VoteSummary> GetVoteSummaryAsync(string tokenId, string? viewerId, CancellationToken cancellationToken)
    {
        List<TokenVote> tokenVotes = await votes.Find(item => item.TokenId == tokenId).ToListAsync(cancellationToken);
        return new VoteSummary { Hot = tokenVotes.LongCount(item => item.Value == VoteValue.Hot), NotHot = tokenVotes.LongCount(item => item.Value == VoteValue.NotHot), ViewerVote = tokenVotes.FirstOrDefault(item => item.UserId == viewerId)?.Value };
    }

    public async Task<TokenSupport> UpsertSupportAsync(TokenSupport support, CancellationToken cancellationToken)
    {
        FilterDefinition<TokenSupport> filter = Builders<TokenSupport>.Filter.Where(item => item.KolUserId == support.KolUserId && item.TokenId == support.TokenId);
        UpdateDefinition<TokenSupport> update = Builders<TokenSupport>.Update
            .Set(item => item.WithdrawnAt, null)
            .SetOnInsert(item => item.Id, support.Id)
            .SetOnInsert(item => item.KolUserId, support.KolUserId)
            .SetOnInsert(item => item.TokenId, support.TokenId)
            .SetOnInsert(item => item.Statement, support.Statement)
            .SetOnInsert(item => item.CreatedAt, support.CreatedAt);
        return await supports.FindOneAndUpdateAsync(operationContext.Session!, filter, update, new FindOneAndUpdateOptions<TokenSupport> { IsUpsert = true, ReturnDocument = ReturnDocument.After }, cancellationToken);
    }

    public async Task<bool> WithdrawSupportAsync(string userId, string tokenId, CancellationToken cancellationToken)
    {
        UpdateResult result = await supports.UpdateOneAsync(operationContext.Session!, item => item.KolUserId == userId && item.TokenId == tokenId && item.WithdrawnAt == null, Builders<TokenSupport>.Update.Set(item => item.WithdrawnAt, DateTimeOffset.UtcNow), cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public Task<PagedResult<TokenSupport>> GetSupportersAsync(string tokenId, bool includeWithdrawn, int limit, int offset, CancellationToken cancellationToken)
    {
        FilterDefinition<TokenSupport> filter = Builders<TokenSupport>.Filter.Eq(item => item.TokenId, tokenId);
        if (!includeWithdrawn)
            filter &= Builders<TokenSupport>.Filter.Eq(item => item.WithdrawnAt, null);
        return PageAsync(supports, filter, limit, offset, cancellationToken);
    }

    public async Task<SocialPost> AddPostAsync(SocialPost post, CancellationToken cancellationToken) { await posts.InsertOneAsync(operationContext.Session!, post, cancellationToken: cancellationToken); return post; }
    public Task<SocialPost?> GetPostAsync(string postId, CancellationToken cancellationToken) => posts.Find(item => item.Id == postId && item.ModerationStatus == ModerationStatus.Published && item.Access == PostAccess.Public).FirstOrDefaultAsync(cancellationToken)!;

    public Task<PagedResult<SocialPost>> GetPostsAsync(string? authorId, string? tokenId, int limit, int offset, CancellationToken cancellationToken)
    {
        FilterDefinition<SocialPost> filter = Builders<SocialPost>.Filter.Where(item => item.ModerationStatus == ModerationStatus.Published && item.Access == PostAccess.Public);
        if (!string.IsNullOrWhiteSpace(authorId))
            filter &= Builders<SocialPost>.Filter.Eq(item => item.AuthorId, authorId);
        if (!string.IsNullOrWhiteSpace(tokenId))
            filter &= Builders<SocialPost>.Filter.Eq(item => item.TokenId, tokenId);
        return PageAsync(posts, filter, limit, offset, cancellationToken);
    }

    public Task<Reputation?> GetReputationAsync(string userId, CancellationToken cancellationToken) => reputations.Find(item => item.UserId == userId).FirstOrDefaultAsync(cancellationToken)!;

    private static async Task<PagedResult<T>> PageAsync<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, int limit, int offset, CancellationToken cancellationToken)
    {
        long total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<T> items = await collection.Find(filter).Skip(offset).Limit(limit).ToListAsync(cancellationToken);
        return new PagedResult<T> { Items = items, Limit = limit, Offset = offset, Total = total };
    }
}
