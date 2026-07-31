using MemeTokenHub.SocialService.Application.Interfaces;
using MemeTokenHub.SocialService.Application.Models;
using MemeTokenHub.SocialService.Domain.Entities;
using MemeTokenHub.SocialService.Domain.Enums;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoSocialRepository(IMongoDatabase database) : ISocialRepository
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
        Follow? existingFollow = await follows.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existingFollow is not null)
            return existingFollow;
        await follows.InsertOneAsync(follow, cancellationToken: cancellationToken);
        return follow;
    }

    public async Task<bool> DeleteFollowAsync(string followerId, FollowTargetType targetType, string targetId, CancellationToken cancellationToken) => (await follows.DeleteOneAsync(item => item.FollowerId == followerId && item.TargetType == targetType && item.TargetId == targetId, cancellationToken)).DeletedCount > 0;

    public async Task<PagedResult<Follow>> GetFollowsAsync(string followerId, FollowTargetType? targetType, int limit, int offset, CancellationToken cancellationToken)
    {
        FilterDefinition<Follow> filter = Builders<Follow>.Filter.Eq(item => item.FollowerId, followerId);
        if (targetType.HasValue)
            filter &= Builders<Follow>.Filter.Eq(item => item.TargetType, targetType.Value);
        return await PageAsync(follows, filter, limit, offset, cancellationToken);
    }

    public async Task<Engagement> AddEngagementAsync(Engagement engagement, CancellationToken cancellationToken)
    {
        if (engagement.Type == "Like")
        {
            Engagement? existingLike = await engagements.Find(item => item.UserId == engagement.UserId && item.TokenId == engagement.TokenId && item.Type == "Like").FirstOrDefaultAsync(cancellationToken);
            if (existingLike is not null)
                return existingLike;
        }
        await engagements.InsertOneAsync(engagement, cancellationToken: cancellationToken);
        return engagement;
    }

    public Task<PagedResult<Engagement>> GetEngagementAsync(string tokenId, int limit, int offset, CancellationToken cancellationToken) => PageAsync(engagements, Builders<Engagement>.Filter.Eq(item => item.TokenId, tokenId), limit, offset, cancellationToken);

    public async Task<TokenVote> UpsertVoteAsync(TokenVote vote, CancellationToken cancellationToken)
    {
        FilterDefinition<TokenVote> filter = Builders<TokenVote>.Filter.Where(item => item.UserId == vote.UserId && item.TokenId == vote.TokenId);
        TokenVote? existingVote = await votes.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existingVote is null)
        { await votes.InsertOneAsync(vote, cancellationToken: cancellationToken); return vote; }
        existingVote.Value = vote.Value;
        existingVote.UpdatedAt = DateTimeOffset.UtcNow;
        await votes.ReplaceOneAsync(filter, existingVote, cancellationToken: cancellationToken);
        return existingVote;
    }

    public async Task<bool> DeleteVoteAsync(string userId, string tokenId, CancellationToken cancellationToken) => (await votes.DeleteOneAsync(item => item.UserId == userId && item.TokenId == tokenId, cancellationToken)).DeletedCount > 0;

    public async Task<VoteSummary> GetVoteSummaryAsync(string tokenId, string? viewerId, CancellationToken cancellationToken)
    {
        List<TokenVote> tokenVotes = await votes.Find(item => item.TokenId == tokenId).ToListAsync(cancellationToken);
        return new VoteSummary { Hot = tokenVotes.LongCount(item => item.Value == VoteValue.Hot), NotHot = tokenVotes.LongCount(item => item.Value == VoteValue.NotHot), ViewerVote = tokenVotes.FirstOrDefault(item => item.UserId == viewerId)?.Value };
    }

    public async Task<TokenSupport> UpsertSupportAsync(TokenSupport support, CancellationToken cancellationToken)
    {
        FilterDefinition<TokenSupport> filter = Builders<TokenSupport>.Filter.Where(item => item.KolUserId == support.KolUserId && item.TokenId == support.TokenId);
        TokenSupport? existingSupport = await supports.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (existingSupport is null)
        { await supports.InsertOneAsync(support, cancellationToken: cancellationToken); return support; }
        existingSupport.WithdrawnAt = null;
        await supports.ReplaceOneAsync(filter, existingSupport, cancellationToken: cancellationToken);
        return existingSupport;
    }

    public async Task<bool> WithdrawSupportAsync(string userId, string tokenId, CancellationToken cancellationToken)
    {
        UpdateResult result = await supports.UpdateOneAsync(item => item.KolUserId == userId && item.TokenId == tokenId && item.WithdrawnAt == null, Builders<TokenSupport>.Update.Set(item => item.WithdrawnAt, DateTimeOffset.UtcNow), cancellationToken: cancellationToken);
        return result.ModifiedCount > 0;
    }

    public Task<PagedResult<TokenSupport>> GetSupportersAsync(string tokenId, bool includeWithdrawn, int limit, int offset, CancellationToken cancellationToken)
    {
        FilterDefinition<TokenSupport> filter = Builders<TokenSupport>.Filter.Eq(item => item.TokenId, tokenId);
        if (!includeWithdrawn)
            filter &= Builders<TokenSupport>.Filter.Eq(item => item.WithdrawnAt, null);
        return PageAsync(supports, filter, limit, offset, cancellationToken);
    }

    public async Task<SocialPost> AddPostAsync(SocialPost post, CancellationToken cancellationToken) { await posts.InsertOneAsync(post, cancellationToken: cancellationToken); return post; }
    public Task<SocialPost?> GetPostAsync(string postId, CancellationToken cancellationToken) => posts.Find(item => item.Id == postId && item.ModerationStatus == ModerationStatus.Published).FirstOrDefaultAsync(cancellationToken)!;

    public Task<PagedResult<SocialPost>> GetPostsAsync(string? authorId, string? tokenId, int limit, int offset, CancellationToken cancellationToken)
    {
        FilterDefinition<SocialPost> filter = Builders<SocialPost>.Filter.Eq(item => item.ModerationStatus, ModerationStatus.Published);
        if (!string.IsNullOrWhiteSpace(authorId))
            filter &= Builders<SocialPost>.Filter.Eq(item => item.AuthorId, authorId);
        if (!string.IsNullOrWhiteSpace(tokenId))
            filter &= Builders<SocialPost>.Filter.Eq(item => item.TokenId, tokenId);
        return PageAsync(posts, filter, limit, offset, cancellationToken);
    }

    public Task<Reputation?> GetReputationAsync(string userId, CancellationToken cancellationToken) => reputations.Find(item => item.UserId == userId).FirstOrDefaultAsync(cancellationToken)!;

    public async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        await follows.Indexes.CreateOneAsync(new CreateIndexModel<Follow>(Builders<Follow>.IndexKeys.Ascending(item => item.FollowerId).Ascending(item => item.TargetType).Ascending(item => item.TargetId), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await votes.Indexes.CreateOneAsync(new CreateIndexModel<TokenVote>(Builders<TokenVote>.IndexKeys.Ascending(item => item.UserId).Ascending(item => item.TokenId), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await supports.Indexes.CreateOneAsync(new CreateIndexModel<TokenSupport>(Builders<TokenSupport>.IndexKeys.Ascending(item => item.KolUserId).Ascending(item => item.TokenId), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
    }

    private static async Task<PagedResult<T>> PageAsync<T>(IMongoCollection<T> collection, FilterDefinition<T> filter, int limit, int offset, CancellationToken cancellationToken)
    {
        long total = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<T> items = await collection.Find(filter).Skip(offset).Limit(limit).ToListAsync(cancellationToken);
        return new PagedResult<T> { Items = items, Limit = limit, Offset = offset, Total = total };
    }
}
