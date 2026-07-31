namespace MemeTokenHub.SocialService.Infrastructure.Persistence;

public sealed class IndexInitializationState
{
    public bool IsInitialized { get; set; }

    public string? LastError { get; set; }
}
