using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Infrastructure.Services;
using Xunit;

namespace ClosetApp.Tests;

public class AiGenerationPreferencesServiceTests
{
    [Fact]
    public async Task GetAsync_WithCurrentUserContext_ReadsUserScopedSettings()
    {
        var tempDir = CreateTempDir();
        var userId = Guid.NewGuid();

        try
        {
            var globalPath = Path.Combine(tempDir, "ai-generation-settings.json");
            await File.WriteAllTextAsync(globalPath, """
            {
              "BaseUrl": "https://global.example/v1",
              "Model": "global-model",
              "TimeoutSeconds": 30
            }
            """);

            var session = new AuthSessionContext();
            var currentUser = new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session);
            session.MarkAuthenticated(userId);
            await currentUser.SetCurrentUserIdAsync(userId);

            var userSettingsDir = Path.Combine(tempDir, "users", userId.ToString("N"));
            Directory.CreateDirectory(userSettingsDir);
            await File.WriteAllTextAsync(Path.Combine(userSettingsDir, "ai-generation-settings.json"), """
            {
              "BaseUrl": "https://user.example/v1",
              "Model": "user-model",
              "TimeoutSeconds": 180
            }
            """);

            var service = new AiGenerationPreferencesService(globalPath, currentUser);

            var preferences = await service.GetAsync();

            Assert.Equal("https://user.example/v1", preferences.BaseUrl);
            Assert.Equal("user-model", preferences.Model);
            Assert.Equal(180, preferences.TimeoutSeconds);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task SaveAsync_WithDifferentCurrentUsers_WritesSeparateSettingsFiles()
    {
        var tempDir = CreateTempDir();
        var firstUserId = Guid.NewGuid();
        var secondUserId = Guid.NewGuid();

        try
        {
            var globalPath = Path.Combine(tempDir, "ai-generation-settings.json");
            var session = new AuthSessionContext();
            var currentUser = new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session);
            var service = new AiGenerationPreferencesService(globalPath, currentUser);

            session.MarkAuthenticated(firstUserId);
            await currentUser.SetCurrentUserIdAsync(firstUserId);
            await service.SaveAsync(new SaveAiGenerationPreferencesRequest("https://first.example/v1", "first-model", 100, null));

            session.MarkAuthenticated(secondUserId);
            await currentUser.SetCurrentUserIdAsync(secondUserId);
            await service.SaveAsync(new SaveAiGenerationPreferencesRequest("https://second.example/v1", "second-model", 200, null));

            var firstJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "users", firstUserId.ToString("N"), "ai-generation-settings.json"));
            var secondJson = await File.ReadAllTextAsync(Path.Combine(tempDir, "users", secondUserId.ToString("N"), "ai-generation-settings.json"));

            Assert.Contains("first-model", firstJson);
            Assert.DoesNotContain("second-model", firstJson);
            Assert.Contains("second-model", secondJson);
            Assert.DoesNotContain("first-model", secondJson);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string path)
    {
        if (Directory.Exists(path))
            Directory.Delete(path, recursive: true);
    }
}
