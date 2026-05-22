using System.IO;
using System.Text.Json;
using ClosetApp.Infrastructure;

namespace ClosetApp.UI.Services;

public sealed class ThemePreferences
{
    public AppThemeKind Theme { get; set; } = AppThemeKind.Rose;
}

public sealed class ThemePreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _filePath;

    public ThemePreferencesService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "theme-settings.json");
    }

    public async Task<ThemePreferences> GetAsync()
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return new ThemePreferences();

            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<ThemePreferences>(stream, JsonOptions).ConfigureAwait(false);
            return Normalize(preferences);
        }
        catch
        {
            return new ThemePreferences();
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task SaveAsync(ThemePreferences preferences)
    {
        var normalized = Normalize(preferences);

        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(_filePath);
            await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions).ConfigureAwait(false);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static ThemePreferences Normalize(ThemePreferences? preferences)
    {
        if (preferences == null)
            return new ThemePreferences();

        return new ThemePreferences
        {
            Theme = Enum.IsDefined(typeof(AppThemeKind), preferences.Theme)
                ? preferences.Theme
                : AppThemeKind.Rose
        };
    }
}
