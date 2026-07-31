using System.Text.Json;

namespace MemeTokenHub.SocialService.IntegrationTests.Configuration;

[TestFixture]
public sealed class AppSettingsTests
{
    [Test]
    public void AppSettings_DefinesRequiredServiceConfiguration()
    {
        string repositoryRoot = FindRepositoryRoot();
        string settingsPath = Path.Combine(repositoryRoot, "src", "MemeTokenHub.SocialService.Api", "appsettings.json");
        using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath));

        Assert.Multiple(() =>
        {
            Assert.That(settings.RootElement.TryGetProperty("MongoDb", out _), Is.True);
            Assert.That(settings.RootElement.TryGetProperty("Jwt", out _), Is.True);
        });
    }

    [Test]
    public void ProductionAppSettings_DoesNotContainJwtSigningSecret()
    {
        string repositoryRoot = FindRepositoryRoot();
        string settingsPath = Path.Combine(repositoryRoot, "src", "MemeTokenHub.SocialService.Api", "appsettings.json");
        using JsonDocument settings = JsonDocument.Parse(File.ReadAllText(settingsPath));

        JsonElement jwtSettings = settings.RootElement.GetProperty("Jwt");
        Assert.That(jwtSettings.TryGetProperty("SecretKey", out _), Is.False);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(TestContext.CurrentContext.TestDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MemeTokenHub.SocialService.sln")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
