using System.IO;
using System.Text.Json;
using System.Threading;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.Infrastructure.Services;
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
    private readonly UserScopedSettingsPath _settingsPath;
    private readonly SynchronizationContext? _synchronizationContext;

    public OutfitDisplayPreferencesService(string? filePath = null, ICurrentUserContext? currentUserContext = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "outfit-display-settings.json");
        _settingsPath = new UserScopedSettingsPath(currentUserContext, _filePath);
        _synchronizationContext = SynchronizationContext.Current;
    }

    public event EventHandler<OutfitDisplayPreferencesChangedEventArgs>? PreferenceChanged;

    public async Task<OutfitDisplayPreferences> GetAsync()
    {
        await Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _settingsPath.MigrateGlobalFileIfNeededAsync().ConfigureAwait(false);
            var path = await _settingsPath.ResolveAsync().ConfigureAwait(false);
            if (!File.Exists(path))
                return new OutfitDisplayPreferences();

            await using var stream = File.OpenRead(path);
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

        await RaisePreferenceChangedAsync(Clone(normalized)).ConfigureAwait(false);
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

    private Task RaisePreferenceChangedAsync(OutfitDisplayPreferences preferences)
    {
        var handler = PreferenceChanged;
        if (handler == null)
            return Task.CompletedTask;

        if (_synchronizationContext == null || ReferenceEquals(SynchronizationContext.Current, _synchronizationContext))
        {
            handler(this, new OutfitDisplayPreferencesChangedEventArgs(preferences));
            return Task.CompletedTask;
        }

        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _synchronizationContext.Post(_ =>
        {
            try
            {
                handler(this, new OutfitDisplayPreferencesChangedEventArgs(preferences));
                completion.SetResult();
            }
            catch (Exception ex)
            {
                completion.SetException(ex);
            }
        }, null);

        return completion.Task;
    }
}
