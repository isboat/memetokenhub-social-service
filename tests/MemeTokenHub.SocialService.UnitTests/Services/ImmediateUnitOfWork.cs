using MemeTokenHub.SocialService.Application.Interfaces;

namespace MemeTokenHub.SocialService.UnitTests.Services;

public sealed class ImmediateUnitOfWork : IUnitOfWork
{
    public Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken) => operation(cancellationToken);

    public Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken) => operation(cancellationToken);
}
