namespace MemeTokenHub.SocialService.Api.Health;

public sealed class HealthCheckResponse
{
    public required string Service { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset CheckedAt { get; init; }

    public required TimeSpan TotalDuration { get; init; }

    public required IReadOnlyCollection<HealthCheckEntryResponse> Checks { get; init; }
}
