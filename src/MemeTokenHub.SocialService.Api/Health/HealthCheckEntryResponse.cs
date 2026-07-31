namespace MemeTokenHub.SocialService.Api.Health;

public sealed class HealthCheckEntryResponse
{
    public required string Name { get; init; }

    public required string Status { get; init; }

    public required TimeSpan Duration { get; init; }

    public string? Description { get; init; }

    public string? Error { get; init; }

    public required IReadOnlyCollection<string> Tags { get; init; }
}
