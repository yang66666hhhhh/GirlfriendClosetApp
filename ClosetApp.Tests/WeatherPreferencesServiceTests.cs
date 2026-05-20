using System.IO;
using ClosetApp.Infrastructure.Services;
using Xunit;

namespace ClosetApp.Tests;

public class WeatherPreferencesServiceTests
{
    [Fact]
    public async Task SaveAsync_ThenGetAsync_PersistsDefaultCity()
    {
        var tempDir = CreateTempDir();

        try
        {
            var filePath = Path.Combine(tempDir, "weather-settings.json");
            var service = new WeatherPreferencesService(filePath);

            await service.SaveAsync(new WeatherPreferences
            {
                DefaultCity = "Hangzhou"
            });

            var saved = await service.GetAsync();

            Assert.Equal("Hangzhou", saved.DefaultCity);
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
            var service = new WeatherPreferencesService(Path.Combine(tempDir, "weather-settings.json"));

            var preferences = await service.GetAsync();

            Assert.Equal("Shanghai", preferences.DefaultCity);
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
