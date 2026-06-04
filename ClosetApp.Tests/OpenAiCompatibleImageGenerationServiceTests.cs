using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure.Services;
using Xunit;

namespace ClosetApp.Tests;

public class OpenAiCompatibleImageGenerationServiceTests
{
    [Fact]
    public async Task TestConnectionAsync_WithoutSavedApiKey_ThrowsValidationError()
    {
        var service = new OpenAiCompatibleImageGenerationService(
            new FakeAiAssetStorageService(),
            new FakeAiGenerationPreferencesService(
                new AiGenerationPreferences(
                    "https://api.openai.com/v1",
                    "gpt-image-1",
                    60,
                    HasEncryptedApiKey: false),
                apiKey: null));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.TestConnectionAsync());

        Assert.Contains("API Key", ex.Message);
    }

    private sealed class FakeAiGenerationPreferencesService : IAiGenerationPreferencesService
    {
        private readonly AiGenerationPreferences _preferences;
        private readonly string? _apiKey;

        public FakeAiGenerationPreferencesService(AiGenerationPreferences preferences, string? apiKey)
        {
            _preferences = preferences;
            _apiKey = apiKey;
        }

        public Task<AiGenerationPreferences> GetAsync() => Task.FromResult(_preferences);
        public Task SaveAsync(SaveAiGenerationPreferencesRequest request) => throw new NotImplementedException();
        public Task<string?> GetApiKeyAsync() => Task.FromResult(_apiKey);
        public Task MarkConnectionCheckedAsync(DateTime checkedAt) => Task.CompletedTask;
    }

    private sealed class FakeAiAssetStorageService : IAiAssetStorageService
    {
        public string GetProfileReferenceFullPath(string relativePath) => relativePath;
        public string GetGeneratedImageFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath) => [relativePath];
        public Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName) => throw new NotImplementedException();
        public Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType) => throw new NotImplementedException();
        public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task TryDeleteProfileReferenceImageAsync(string? imagePath) => Task.CompletedTask;
        public Task TryDeleteGeneratedImageAsync(string? imagePath) => Task.CompletedTask;
    }
}
