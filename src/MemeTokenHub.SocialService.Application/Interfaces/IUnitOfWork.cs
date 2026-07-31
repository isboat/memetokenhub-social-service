namespace MemeTokenHub.SocialService.Application.Interfaces;

public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
