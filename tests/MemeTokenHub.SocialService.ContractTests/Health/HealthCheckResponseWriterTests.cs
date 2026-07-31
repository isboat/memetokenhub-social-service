using System.Text.Json;
using MemeTokenHub.SocialService.Api.Health;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MemeTokenHub.SocialService.ContractTests.Health;

[TestFixture]
public sealed class HealthCheckResponseWriterTests
{
    [Test]
    public async Task WriteAsync_ReturnsDashboardFriendlyHealthDetails()
    {
        Dictionary<string, HealthReportEntry> entries = new()
        {
            ["mongodb"] = new HealthReportEntry(
                HealthStatus.Healthy,
                "MongoDB is available.",
                TimeSpan.FromMilliseconds(12),
                null,
                null,
                ["ready", "dependency", "database"]),
        };
        HealthReport report = new(entries, TimeSpan.FromMilliseconds(15));
        DefaultHttpContext context = new();
        context.Response.Body = new MemoryStream();

        await HealthCheckResponseWriter.WriteAsync(context, report);

        context.Response.Body.Position = 0;
        using JsonDocument response = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Multiple(() =>
        {
            Assert.That(context.Response.ContentType, Is.EqualTo("application/json; charset=utf-8"));
            Assert.That(response.RootElement.GetProperty("service").GetString(), Is.EqualTo("memetokenhub-social-service"));
            Assert.That(response.RootElement.GetProperty("status").GetString(), Is.EqualTo("Healthy"));
            Assert.That(response.RootElement.GetProperty("checks")[0].GetProperty("name").GetString(), Is.EqualTo("mongodb"));
        });
    }
}
