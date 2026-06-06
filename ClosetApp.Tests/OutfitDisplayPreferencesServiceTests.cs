using System.IO;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitDisplayPreferencesServiceTests
{
    [Fact]
    public async Task GetAsync_WithMissingFile_ReturnsDefaultPreferences()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var service = new OutfitDisplayPreferencesService(Path.Combine(tempDir, "outfit-display-settings.json"));

            var result = await service.GetAsync();

            Assert.Equal(OutfitCardDisplayMode.OutfitFirst, result.DefaultCardDisplayMode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GetAsync_WithInvalidEnumValue_NormalizesToDefault()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "outfit-display-settings.json");
            await File.WriteAllTextAsync(filePath, "{\n  \"DefaultCardDisplayMode\": 99\n}");
            var service = new OutfitDisplayPreferencesService(filePath);

            var result = await service.GetAsync();

            Assert.Equal(OutfitCardDisplayMode.OutfitFirst, result.DefaultCardDisplayMode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenGetAsync_PersistsDisplayMode()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var filePath = Path.Combine(tempDir, "outfit-display-settings.json");
            var service = new OutfitDisplayPreferencesService(filePath);

            await service.SaveAsync(new OutfitDisplayPreferences
            {
                DefaultCardDisplayMode = OutfitCardDisplayMode.EffectImageFirst
            });

            var result = await service.GetAsync();

            Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, result.DefaultCardDisplayMode);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
