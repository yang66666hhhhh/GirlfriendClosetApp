using System.IO;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using Xunit;

namespace ClosetApp.Tests;

public class RecommendationPreferencesServiceTests
{
    [Fact]
    public async Task SaveAsync_ThenGetAsync_PersistsRecommendationPreferences()
    {
        var tempDir = CreateTempDir();

        try
        {
            var filePath = Path.Combine(tempDir, "recommendation-settings.json");
            var service = new RecommendationPreferencesService(filePath);

            await service.SaveAsync(new RecommendationPreferences
            {
                DefaultScene = OutfitScene.Work,
                AvoidWornToday = false,
                RotationStrategy = RecommendationRotationStrategy.PreferFavorites
            });

            var saved = await service.GetAsync();

            Assert.Equal(OutfitScene.Work, saved.DefaultScene);
            Assert.False(saved.AvoidWornToday);
            Assert.Equal(RecommendationRotationStrategy.PreferFavorites, saved.RotationStrategy);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_WithMissingFile_ReturnsDefaultPreferences()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new RecommendationPreferencesService(Path.Combine(tempDir, "recommendation-settings.json"));

            var preferences = await service.GetAsync();

            Assert.Null(preferences.DefaultScene);
            Assert.True(preferences.AvoidWornToday);
            Assert.Equal(RecommendationRotationStrategy.Balanced, preferences.RotationStrategy);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "ClosetAppTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
