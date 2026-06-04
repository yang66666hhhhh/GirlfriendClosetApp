using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.Services;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Infrastructure.Services;

public sealed class OpenAiCompatibleImageGenerationService : IAiImageGenerationService
{
    private readonly IAiAssetStorageService _assetStorageService;
    private readonly IAiGenerationPreferencesService _preferencesService;

    public OpenAiCompatibleImageGenerationService(
        IAiAssetStorageService assetStorageService,
        IAiGenerationPreferencesService preferencesService)
    {
        _assetStorageService = assetStorageService;
        _preferencesService = preferencesService;
    }

    public async Task TestConnectionAsync()
    {
        var preferences = await _preferencesService.GetAsync();
        var apiKey = await _preferencesService.GetApiKeyAsync();

        if (string.IsNullOrWhiteSpace(preferences.BaseUrl))
            throw new InvalidOperationException("还没有配置 Base URL。");

        if (string.IsNullOrWhiteSpace(preferences.Model))
            throw new InvalidOperationException("还没有配置模型。");

        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("还没有保存 API Key，请先保存配置。");

        using var client = CreateHttpClient(preferences, apiKey);
        using var request = new HttpRequestMessage(HttpMethod.Get, preferences.BaseUrl.TrimEnd('/') + "/models");
        using var response = await client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw CreateProviderException(response.StatusCode, body);
    }

    public async Task<AiImageGenerationResponse> GenerateOutfitEffectImageAsync(
        PersonalProfile profile,
        Outfit outfit,
        GenerateOutfitEffectImageRequest request,
        AiGenerationPreferences preferences,
        string apiKey)
    {
        using var client = CreateHttpClient(preferences, apiKey);

        if (string.IsNullOrWhiteSpace(profile.AvatarPhotoPath))
            throw new InvalidOperationException("生成效果图至少需要一张头像照。");

        var avatarPath = _assetStorageService.GetProfileReferenceFullPath(profile.AvatarPhotoPath);
        var referenceImages = new List<string>
        {
            avatarPath
        };

        if (!string.IsNullOrWhiteSpace(profile.FullBodyPhotoPath))
        {
            var fullBodyPath = _assetStorageService.GetProfileReferenceFullPath(profile.FullBodyPhotoPath);
            referenceImages.Add(fullBodyPath);
        }

        var prompt = AiGenerationPromptBuilder.BuildOutfitEffectPrompt(profile, outfit, request);
        var profileSnapshot = BuildProfileSnapshot(profile);
        var outfitSnapshot = BuildOutfitSnapshot(outfit);
        var optionSnapshot = JsonSerializer.Serialize(request);

        var endpoint = preferences.BaseUrl.TrimEnd('/') + "/images/edits";
        using var content = BuildImageEditContent(referenceImages, prompt, preferences.Model);
        using var response = await client.PostAsync(endpoint, content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw CreateProviderException(response.StatusCode, body);

        var document = JsonDocument.Parse(body);
        var base64 = document.RootElement
            .GetProperty("data")[0]
            .TryGetProperty("b64_json", out var base64Node)
            ? base64Node.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("图片服务返回成功，但响应里没有图片数据。");

        return new AiImageGenerationResponse(
            "OpenAI-Compatible",
            preferences.Model,
            prompt,
            Convert.FromBase64String(base64),
            "image/png",
            profileSnapshot,
            outfitSnapshot,
            optionSnapshot);
    }

    private static MultipartFormDataContent BuildImageEditContent(
        IReadOnlyList<string> imagePaths,
        string prompt,
        string model)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(model, Encoding.UTF8), "model");
        content.Add(new StringContent(prompt, Encoding.UTF8), "prompt");
        content.Add(new StringContent("1024x1536", Encoding.UTF8), "size");

        foreach (var imagePath in imagePaths)
        {
            var bytes = File.ReadAllBytes(imagePath);
            var imageContent = new ByteArrayContent(bytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(ResolveImageMimeType(imagePath));
            content.Add(imageContent, "image[]", Path.GetFileName(imagePath));
        }

        return content;
    }

    private static string BuildProfileSnapshot(PersonalProfile profile)
    {
        return JsonSerializer.Serialize(new
        {
            profile.DisplayName,
            profile.HeightCm,
            profile.BodyShape,
            profile.SkinTone,
            profile.HairLength,
            profile.HairColor,
            profile.FaceFeaturesSummary,
            profile.StyleKeywords,
            profile.AvoidKeywords,
            profile.AvatarPhotoPath,
            profile.FullBodyPhotoPath
        });
    }

    private static string BuildOutfitSnapshot(Outfit outfit)
    {
        return JsonSerializer.Serialize(new
        {
            outfit.Id,
            outfit.Name,
            outfit.Scene,
            outfit.Season,
            outfit.Rating,
            Clothes = outfit.OutfitClothes
                .Where(link => link.Clothing != null)
                .Select(link => new
                {
                    link.ClothingId,
                    link.Clothing!.Name,
                    link.Clothing.Color,
                    link.Clothing.Brand,
                    link.Clothing.Type,
                    link.Clothing.GarmentType,
                    Tags = link.Clothing.ClothingTags.Select(tag => tag.Tag?.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList()
                })
                .ToList()
        });
    }

    private static Exception CreateProviderException(HttpStatusCode statusCode, string body)
    {
        var detail = TryExtractProviderMessage(body) ?? body;
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new InvalidOperationException($"AI 服务鉴权失败：{detail}"),
            HttpStatusCode.Forbidden => new InvalidOperationException($"AI 服务拒绝访问：{detail}"),
            HttpStatusCode.TooManyRequests => new InvalidOperationException($"AI 服务当前限流或余额不足：{detail}"),
            HttpStatusCode.RequestTimeout => new TimeoutException($"AI 服务请求超时：{detail}"),
            _ => new InvalidOperationException($"AI 服务返回错误：{detail}")
        };
    }

    private static HttpClient CreateHttpClient(AiGenerationPreferences preferences, string apiKey)
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(30, preferences.TimeoutSeconds))
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        return client;
    }

    private static string ResolveImageMimeType(string imagePath)
    {
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            _ => "application/octet-stream"
        };
    }

    private static string? TryExtractProviderMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
                    return message.GetString();

                if (error.ValueKind == JsonValueKind.String)
                    return error.GetString();
            }
        }
        catch
        {
            // Ignore malformed provider payloads and fall back to raw body.
        }

        return null;
    }
}
