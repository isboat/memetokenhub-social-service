using MemeTokenHub.SocialService.Domain.Entities;
using MemeTokenHub.SocialService.Infrastructure.Messaging;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoIndexInitializer(IMongoDatabase database) : IMongoIndexInitializer
{
    public async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<Follow> follows = database.GetCollection<Follow>("Follows");
        IMongoCollection<TokenVote> votes = database.GetCollection<TokenVote>("Votes");
        IMongoCollection<TokenSupport> supports = database.GetCollection<TokenSupport>("TokenSupports");
        IMongoCollection<Engagement> engagements = database.GetCollection<Engagement>("Engagements");
        IMongoCollection<OutboxMessage> outboxMessages = database.GetCollection<OutboxMessage>("EventOutbox");

        await follows.Indexes.CreateOneAsync(new CreateIndexModel<Follow>(Builders<Follow>.IndexKeys.Ascending(item => item.FollowerId).Ascending(item => item.TargetType).Ascending(item => item.TargetId), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await votes.Indexes.CreateOneAsync(new CreateIndexModel<TokenVote>(Builders<TokenVote>.IndexKeys.Ascending(item => item.UserId).Ascending(item => item.TokenId), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await supports.Indexes.CreateOneAsync(new CreateIndexModel<TokenSupport>(Builders<TokenSupport>.IndexKeys.Ascending(item => item.KolUserId).Ascending(item => item.TokenId), new CreateIndexOptions { Unique = true }), cancellationToken: cancellationToken);
        await engagements.Indexes.CreateOneAsync(
            new CreateIndexModel<Engagement>(
                Builders<Engagement>.IndexKeys.Ascending(item => item.UserId).Ascending(item => item.TokenId).Ascending(item => item.Type),
                new CreateIndexOptions<Engagement>
                {
                    Unique = true,
                    PartialFilterExpression = Builders<Engagement>.Filter.Eq(item => item.Type, "Like"),
                }),
            cancellationToken: cancellationToken);
        await outboxMessages.Indexes.CreateOneAsync(
            new CreateIndexModel<OutboxMessage>(
                Builders<OutboxMessage>.IndexKeys
                    .Ascending(item => item.PublishedAt)
                    .Ascending(item => item.DeadLetteredAt)
                    .Ascending(item => item.OccurredAt)),
            cancellationToken: cancellationToken);
    }
}
