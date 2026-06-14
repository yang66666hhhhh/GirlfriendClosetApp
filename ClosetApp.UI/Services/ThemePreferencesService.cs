using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.Infrastructure.Services;

namespace ClosetApp.UI.Services;

public sealed class ThemePreferences
{
    public AppThemeKind Theme { get; set; } = AppThemeKind.Rose;
    public AppFontSizeLevel FontSizeLevel { get; set; } = AppFontSizeLevel.Standard;
}

public sealed class ThemePreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _filePath;
    private readonly UserScopedSettingsPath _settingsPath;

    public ThemePreferencesService(string? filePath = null, ICurrentUserContext? currentUserContext = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "theme-settings.json");
        _settingsPath = new UserScopedSettingsPath(currentUserContext, _filePath);
    }

    public async Task<ThemePreferences> GetAsync()
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _settingsPath.MigrateGlobalFileIfNeededAsync().ConfigureAwait(false);
            var path = await _settingsPath.ResolveAsync().ConfigureAwait(false);
            if (!File.Exists(path))
                return new ThemePreferences();

            await using var stream = File.OpenRead(path);
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
            var path = await _settingsPath.ResolveAsync().ConfigureAwait(false);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(path);
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
                : AppThemeKind.Rose,
            FontSizeLevel = Enum.IsDefined(typeof(AppFontSizeLevel), preferences.FontSizeLevel)
                ? preferences.FontSizeLevel
                : AppFontSizeLevel.Standard
        };
    }
}
