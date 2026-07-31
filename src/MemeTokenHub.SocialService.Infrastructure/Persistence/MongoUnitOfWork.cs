using MemeTokenHub.SocialService.Application.Interfaces;
using MongoDB.Driver;

namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class MongoUnitOfWork(
    IMongoClient mongoClient,
    IMongoOperationContext operationContext) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (operationContext.Session is not null)
        {
            return await operation(cancellationToken);
        }

        using IClientSessionHandle session = await mongoClient.StartSessionAsync(cancellationToken: cancellationToken);
        operationContext.Session = session;

        try
        {
            return await session.WithTransactionAsync(
                (_, transactionCancellationToken) => operation(transactionCancellationToken),
                cancellationToken: cancellationToken);
        }
        finally
        {
            operationContext.Session = null;
        }
    }

    public Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken) => ExecuteAsync(
            async transactionCancellationToken =>
            {
                await operation(transactionCancellationToken);
                return true;
            },
            cancellationToken);
}
