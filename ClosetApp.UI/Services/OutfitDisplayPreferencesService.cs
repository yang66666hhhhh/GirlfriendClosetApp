using System.IO;
using System.Text.Json;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Logic.Services;

namespace ClosetApp.UI.Services;

public sealed class OutfitDisplayPreferences
{
    public OutfitCardDisplayMode DefaultCardDisplayMode { get; set; } = OutfitCardDisplayMode.OutfitFirst;
}

public sealed class OutfitDisplayPreferencesChangedEventArgs : EventArgs
{
    public OutfitDisplayPreferencesChangedEventArgs(OutfitDisplayPreferences preferences)
    {
        Preferences = preferences;
    }

    public OutfitDisplayPreferences Preferences { get; }
}

public sealed class OutfitDisplayPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _filePath;

    public OutfitDisplayPreferencesService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "outfit-display-settings.json");
    }

    public event EventHandler<OutfitDisplayPreferencesChangedEventArgs>? PreferenceChanged;

    public async Task<OutfitDisplayPreferences> GetAsync()
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!File.Exists(_filePath))
                return new OutfitDisplayPreferences();

            await using var stream = File.OpenRead(_filePath);
            var preferences = await JsonSerializer.DeserializeAsync<OutfitDisplayPreferences>(stream, JsonOptions).ConfigureAwait(false);
            return Normalize(preferences);
        }
        catch
        {
            return new OutfitDisplayPreferences();
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task SaveAsync(OutfitDisplayPreferences preferences)
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

        PreferenceChanged?.Invoke(this, new OutfitDisplayPreferencesChangedEventArgs(Clone(normalized)));
    }

    private static OutfitDisplayPreferences Normalize(OutfitDisplayPreferences? preferences)
    {
        if (preferences == null)
            return new OutfitDisplayPreferences();

        return new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = Enum.IsDefined(typeof(OutfitCardDisplayMode), preferences.DefaultCardDisplayMode)
                ? preferences.DefaultCardDisplayMode
                : OutfitCardDisplayMode.OutfitFirst
        };
    }

    private static OutfitDisplayPreferences Clone(OutfitDisplayPreferences preferences)
    {
        return new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = preferences.DefaultCardDisplayMode
        };
    }
}
