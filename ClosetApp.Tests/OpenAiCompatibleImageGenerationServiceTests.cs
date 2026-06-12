using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
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

    [Fact]
    public async Task GenerateOutfitEffectImageAsync_WithResponsesModel_CompressesLargeReferenceImages()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var avatarPath = Path.Combine(tempDir, "avatar.png");
        var fullBodyPath = Path.Combine(tempDir, "full-body.png");

        try
        {
            using (var avatar = new Image<Rgba32>(2400, 2400))
            {
                await avatar.SaveAsPngAsync(avatarPath);
            }

            using (var fullBody = new Image<Rgba32>(1800, 2600))
            {
                await fullBody.SaveAsPngAsync(fullBodyPath);
            }

            var preferences = new AiGenerationPreferences(
                "https://api.openai.com/v1",
                "gpt-5.4",
                120,
                HasEncryptedApiKey: true);
            var handler = new CapturingHttpMessageHandler();
            var service = new OpenAiCompatibleImageGenerationService(
                new FakeAiAssetStorageService(),
                new FakeAiGenerationPreferencesService(preferences, "test-key"),
                () => handler);

            var response = await service.GenerateOutfitEffectImageAsync(
                new ClosetApp.Domain.Entities.PersonalProfile
                {
                    AvatarPhotoPath = avatarPath,
                    FullBodyPhotoPath = fullBodyPath,
                    DisplayName = "测试",
                    CloudUploadConsentAcceptedAt = DateTime.Now
                },
                new ClosetApp.Domain.Entities.Outfit
                {
                    Name = "通勤搭配"
                },
                new GenerateOutfitEffectImageRequest(Guid.NewGuid(), "通勤", "站姿正面", "城市街景", "全身", "松弛"),
                preferences,
                "test-key");

            Assert.Equal("image/png", response.MimeType);
            Assert.NotNull(handler.CapturedPayload);
            Assert.Contains("\"model\":\"gpt-5.4\"", handler.CapturedPayload!, StringComparison.Ordinal);

            var originalAvatarBytes = File.ReadAllBytes(avatarPath);
            using var document = JsonDocument.Parse(handler.CapturedPayload!);
            var images = document.RootElement
                .GetProperty("input")[0]
                .GetProperty("content")
                .EnumerateArray()
                .Where(item => item.TryGetProperty("type", out var type) && type.GetString() == "input_image")
                .Select(item => item.GetProperty("image_url").GetString())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();

            Assert.Equal(2, images.Count);

            foreach (var imageUrl in images)
            {
                Assert.StartsWith("data:image/jpeg;base64,", imageUrl!, StringComparison.Ordinal);
                var bytes = Convert.FromBase64String(imageUrl!["data:image/jpeg;base64,".Length..]);
                Assert.True(bytes.Length < originalAvatarBytes.Length);
            }
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task GenerateOutfitEffectImageAsync_WithGptImage2_UsesImageGenerationsWithoutReferenceImages()
    {
        var preferences = new AiGenerationPreferences(
            "https://api.example.com/v1",
            "gpt-image-2",
            180,
            HasEncryptedApiKey: true);
        var handler = new CapturingHttpMessageHandler("""
        {
          "data": [
            {
              "b64_json": "AQID"
            }
          ]
        }
        """);
        var service = new OpenAiCompatibleImageGenerationService(
            new FakeAiAssetStorageService(),
            new FakeAiGenerationPreferencesService(preferences, "test-key"),
            () => handler);

        var response = await service.GenerateOutfitEffectImageAsync(
            new ClosetApp.Domain.Entities.PersonalProfile
            {
                DisplayName = "测试",
                CloudUploadConsentAcceptedAt = DateTime.Now
            },
            new ClosetApp.Domain.Entities.Outfit
            {
                Name = "文生图搭配"
            },
            new GenerateOutfitEffectImageRequest(Guid.NewGuid(), "通勤", "站姿正面", "城市街景", "全身", "松弛"),
            preferences,
            "test-key");

        Assert.Equal("image/png", response.MimeType);
        Assert.Equal("https://api.example.com/v1/images/generations", handler.CapturedRequestUri);
        Assert.NotNull(handler.CapturedPayload);

        using var document = JsonDocument.Parse(handler.CapturedPayload!);
        Assert.Equal("gpt-image-2", document.RootElement.GetProperty("model").GetString());
        Assert.True(document.RootElement.TryGetProperty("prompt", out _));
        Assert.False(document.RootElement.TryGetProperty("image", out _));
        Assert.False(document.RootElement.TryGetProperty("image[]", out _));
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
        public string GetProfileReferenceFullPath(string relativePath, Guid? userId = null) => relativePath;
        public string GetGeneratedImageFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath) => [relativePath];
        public Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName, Guid? userId = null) => throw new NotImplementedException();
        public Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType) => throw new NotImplementedException();
        public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName, Guid? userId = null) => throw new NotImplementedException();
        public Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task TryDeleteProfileReferenceImageAsync(string? imagePath, Guid? userId = null) => Task.CompletedTask;
        public Task TryDeleteGeneratedImageAsync(string? imagePath) => Task.CompletedTask;
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly string _responsePayload;

        public CapturingHttpMessageHandler(string? responsePayload = null)
        {
            _responsePayload = responsePayload ?? JsonSerializer.Serialize(new
            {
                output = new[]
                {
                    new
                    {
                        content = new[]
                        {
                            new
                            {
                                type = "output_image",
                                image_base64 = Convert.ToBase64String([1, 2, 3])
                            }
                        }
                    }
                }
            });
        }

        public string? CapturedPayload { get; private set; }
        public string? CapturedRequestUri { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CapturedRequestUri = request.RequestUri?.ToString();
            CapturedPayload = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_responsePayload, Encoding.UTF8, "application/json")
            };
        }
    }
}
