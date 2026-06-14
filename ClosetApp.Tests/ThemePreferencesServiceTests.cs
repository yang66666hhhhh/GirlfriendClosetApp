using System.IO;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public class ThemePreferencesServiceTests
{
    [Fact]
    public async Task GetAsync_WithLegacyThemeOnlyJson_UsesStandardFontSize()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, "{\"Theme\":\"Blue\"}");
        var service = new ThemePreferencesService(filePath);

        var preferences = await service.GetAsync();

        Assert.Equal(AppThemeKind.Blue, preferences.Theme);
        Assert.Equal(AppFontSizeLevel.Standard, preferences.FontSizeLevel);
    }

    [Fact]
    public async Task GetAsync_WithInvalidFontSizeLevel_UsesStandardFontSize()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(filePath, "{\"Theme\":\"Rose\",\"FontSizeLevel\":99}");
        var service = new ThemePreferencesService(filePath);

        var preferences = await service.GetAsync();

        Assert.Equal(AppThemeKind.Rose, preferences.Theme);
        Assert.Equal(AppFontSizeLevel.Standard, preferences.FontSizeLevel);
    }

    [Fact]
    public async Task SaveAsync_PersistsFontSizeLevel()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json");
        var service = new ThemePreferencesService(filePath);

        await service.SaveAsync(new ThemePreferences
        {
            Theme = AppThemeKind.Blue,
            FontSizeLevel = AppFontSizeLevel.Large
        });

        var savedJson = await File.ReadAllTextAsync(filePath);
        Assert.Contains(nameof(ThemePreferences.FontSizeLevel), savedJson);
        var preferences = await service.GetAsync();
        Assert.Equal(AppThemeKind.Blue, preferences.Theme);
        Assert.Equal(AppFontSizeLevel.Large, preferences.FontSizeLevel);
    }
}
