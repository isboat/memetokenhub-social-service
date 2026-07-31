using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.SocialService.Api.Health;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        HealthCheckResponse response = new()
        {
            Service = "memetokenhub-social-service",
            Status = report.Status.ToString(),
            CheckedAt = DateTimeOffset.UtcNow,
            TotalDuration = report.TotalDuration,
            Checks = report.Entries.Select(entry => new HealthCheckEntryResponse
            {
                Name = entry.Key,
                Status = entry.Value.Status.ToString(),
                Duration = entry.Value.Duration,
                Description = entry.Value.Description,
                Error = entry.Value.Exception is null ? null : "Dependency check failed.",
                Tags = entry.Value.Tags.Order(StringComparer.Ordinal).ToArray(),
            }).OrderBy(entry => entry.Name, StringComparer.Ordinal).ToArray(),
        };

        context.Response.ContentType = "application/json; charset=utf-8";
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, SerializerOptions));
    }
}
