using ClosetApp.Application.DTOs;

namespace ClosetApp.Application.Interfaces;

public interface IAiGenerationPreferencesService
{
    Task<AiGenerationPreferences> GetAsync();
    Task SaveAsync(SaveAiGenerationPreferencesRequest request);
    Task<string?> GetApiKeyAsync();
    Task MarkConnectionCheckedAsync(DateTime checkedAt);
}
