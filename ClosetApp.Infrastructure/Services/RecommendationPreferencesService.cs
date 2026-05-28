using System.Text.Json;
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

    public RecommendationPreferencesService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "recommendation-settings.json");
    }

    public async Task<RecommendationPreferences> GetAsync()
    {
        await Gate.WaitAsync();
        try
        {
            if (!File.Exists(_filePath))
                return CreateDefaultPreferences();

            await using var stream = File.OpenRead(_filePath);
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
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var stream = File.Create(_filePath);
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
