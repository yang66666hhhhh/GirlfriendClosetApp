using System.Text.Json;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Infrastructure.Services;

public class RecommendationPreferences
{
    public OutfitScene? DefaultScene { get; set; }
    public bool AvoidWornToday { get; set; } = true;
    public RecommendationRotationStrategy RotationStrategy { get; set; } = RecommendationRotationStrategy.Balanced;
}

public interface IRecommendationPreferencesService
{
    Task<RecommendationPreferences> GetAsync();
    Task SaveAsync(RecommendationPreferences preferences);
}

public class RecommendationPreferencesService : IRecommendationPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly SemaphoreSlim Gate = new(1, 1);
    private readonly string _filePath;
    private readonly UserScopedSettingsPath _settingsPath;

    public RecommendationPreferencesService(string? filePath = null, ICurrentUserContext? currentUserContext = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "recommendation-settings.json");
        _settingsPath = new UserScopedSettingsPath(currentUserContext, _filePath);
    }

    public async Task<RecommendationPreferences> GetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            await _settingsPath.MigrateGlobalFileIfNeededAsync();
            var path = await _settingsPath.ResolveAsync();
            if (!File.Exists(path))
                return CreateDefaultPreferences();

            await using var stream = File.OpenRead(path);
            var preferences = await JsonSerializer.DeserializeAsync<RecommendationPreferences>(stream, JsonOptions);
            return Normalize(preferences);
        }
        catch
        {
            return CreateDefaultPreferences();
        }
        finally
        {
            Gate.Release();
        }
    }

    public async Task SaveAsync(RecommendationPreferences preferences)
    {
        var normalized = Normalize(preferences);

        await Gate.WaitAsync();
        try
        {
            var path = await _settingsPath.ResolveAsync();
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(path);
            await JsonSerializer.SerializeAsync(stream, normalized, JsonOptions);
        }
        finally
        {
            Gate.Release();
        }
    }

    private static RecommendationPreferences CreateDefaultPreferences()
    {
        return new RecommendationPreferences();
    }

    private static RecommendationPreferences Normalize(RecommendationPreferences? preferences)
    {
        return new RecommendationPreferences
        {
            DefaultScene = preferences?.DefaultScene,
            AvoidWornToday = preferences?.AvoidWornToday ?? true,
            RotationStrategy = preferences?.RotationStrategy ?? RecommendationRotationStrategy.Balanced
        };
    }
}
