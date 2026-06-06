using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.Services;
using ClosetApp.Domain.Entities;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ClosetApp.Infrastructure.Services;

public sealed class OpenAiCompatibleImageGenerationService : IAiImageGenerationService
{
    private const string ResponsesImageTool = "image_generation";
    private const int GenerateRetryCount = 1;
    private const int MaxResponsesReferenceImageSide = 768;
    private const int ResponsesReferenceImageQuality = 72;
    private const string DefaultGenerationSize = "1024x1536";
    private readonly IAiAssetStorageService _assetStorageService;
    private readonly IAiGenerationPreferencesService _preferencesService;
    private readonly Func<HttpMessageHandler?>? _httpMessageHandlerFactory;

    public OpenAiCompatibleImageGenerationService(
        IAiAssetStorageService assetStorageService,
        IAiGenerationPreferencesService preferencesService,
        Func<HttpMessageHandler?>? httpMessageHandlerFactory = null)
    {
        _assetStorageService = assetStorageService;
        _preferencesService = preferencesService;
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
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

        using var client = CreateHttpClient(preferences, apiKey, _httpMessageHandlerFactory);
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildEndpoint(client, "models"));
        using var response = await SendWithRetryAsync(
            () => new HttpRequestMessage(HttpMethod.Get, BuildEndpoint(client, "models")),
            requestMessage => client.SendAsync(requestMessage),
            retryCount: 1);
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
        using var client = CreateHttpClient(preferences, apiKey, _httpMessageHandlerFactory);

        var prompt = AiGenerationPromptBuilder.BuildOutfitEffectPrompt(profile, outfit, request);
        var profileSnapshot = BuildProfileSnapshot(profile);
        var outfitSnapshot = BuildOutfitSnapshot(outfit);
        var optionSnapshot = JsonSerializer.Serialize(request);

        if (ShouldUseResponsesApi(preferences.Model))
        {
            var referenceImages = BuildReferenceImages(profile, requireAvatar: true);
            return await GenerateWithResponsesAsync(
                client,
                referenceImages,
                prompt,
                preferences.Model,
                profileSnapshot,
                outfitSnapshot,
                optionSnapshot);
        }

        if (ShouldUseImageGenerationApi(preferences.Model))
        {
            return await GenerateWithImageGenerationsAsync(
                client,
                prompt,
                preferences.Model,
                profileSnapshot,
                outfitSnapshot,
                optionSnapshot);
        }

        var editReferenceImages = BuildReferenceImages(profile, requireAvatar: true);
        return await GenerateWithImageEditsAsync(
            client,
            editReferenceImages,
            prompt,
            preferences.Model,
            profileSnapshot,
            outfitSnapshot,
            optionSnapshot);
    }

    private async Task<AiImageGenerationResponse> GenerateWithImageEditsAsync(
        HttpClient client,
        IReadOnlyList<string> referenceImages,
        string prompt,
        string model,
        string profileSnapshot,
        string outfitSnapshot,
        string optionSnapshot)
    {
        var endpoint = BuildEndpoint(client, "images/edits");
        using var response = await SendWithRetryAsync(
            () => BuildImageEditRequest(endpoint, referenceImages, prompt, model),
            requestMessage => client.SendAsync(requestMessage),
            GenerateRetryCount);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw CreateProviderException(response.StatusCode, body);

        var base64 = TryExtractImageBase64FromImageEdits(body);
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("图片服务返回成功，但响应里没有图片数据。");

        return new AiImageGenerationResponse(
            "OpenAI-Compatible",
            model,
            prompt,
            Convert.FromBase64String(base64),
            "image/png",
            profileSnapshot,
            outfitSnapshot,
            optionSnapshot);
    }

    private async Task<AiImageGenerationResponse> GenerateWithImageGenerationsAsync(
        HttpClient client,
        string prompt,
        string model,
        string profileSnapshot,
        string outfitSnapshot,
        string optionSnapshot)
    {
        var endpoint = BuildEndpoint(client, "images/generations");
        using var response = await SendWithRetryAsync(
            () => BuildImageGenerationRequest(endpoint, prompt, model),
            requestMessage => client.SendAsync(requestMessage),
            GenerateRetryCount);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw CreateProviderException(response.StatusCode, body);

        var base64 = TryExtractImageBase64FromImageEdits(body);
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("图片服务返回成功，但 generations 结果里没有图片数据。");

        return new AiImageGenerationResponse(
            "OpenAI-Compatible",
            model,
            prompt,
            Convert.FromBase64String(base64),
            "image/png",
            profileSnapshot,
            outfitSnapshot,
            optionSnapshot);
    }

    private async Task<AiImageGenerationResponse> GenerateWithResponsesAsync(
        HttpClient client,
        IReadOnlyList<string> referenceImages,
        string prompt,
        string model,
        string profileSnapshot,
        string outfitSnapshot,
        string optionSnapshot)
    {
        var endpoint = BuildEndpoint(client, "responses");
        using var response = await SendWithRetryAsync(
            () => BuildResponsesRequest(endpoint, referenceImages, prompt, model),
            requestMessage => client.SendAsync(requestMessage),
            GenerateRetryCount);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw CreateProviderException(response.StatusCode, body);

        var base64 = TryExtractImageBase64FromResponses(body);
        if (string.IsNullOrWhiteSpace(base64))
            throw new InvalidOperationException("图片服务返回成功，但 responses 结果里没有图片数据。");

        return new AiImageGenerationResponse(
            "OpenAI-Compatible",
            model,
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
        content.Add(new StringContent(DefaultGenerationSize, Encoding.UTF8), "size");

        foreach (var imagePath in imagePaths)
        {
            var bytes = File.ReadAllBytes(imagePath);
            var imageContent = new ByteArrayContent(bytes);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(ResolveImageMimeType(imagePath));
            content.Add(imageContent, "image[]", Path.GetFileName(imagePath));
        }

        return content;
    }

    private static HttpRequestMessage BuildImageEditRequest(
        string endpoint,
        IReadOnlyList<string> imagePaths,
        string prompt,
        string model)
    {
        return new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = BuildImageEditContent(imagePaths, prompt, model)
        };
    }

    private static StringContent BuildImageGenerationContent(string prompt, string model)
    {
        var payload = new
        {
            model,
            prompt,
            size = DefaultGenerationSize
        };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
    }

    private static HttpRequestMessage BuildImageGenerationRequest(
        string endpoint,
        string prompt,
        string model)
    {
        return new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = BuildImageGenerationContent(prompt, model)
        };
    }

    private static StringContent BuildResponsesContent(
        IReadOnlyList<string> imagePaths,
        string prompt,
        string model)
    {
        var input = new List<object>
        {
            new
            {
                role = "user",
                content = BuildResponsesInputContent(imagePaths, prompt)
            }
        };

        var payload = new
        {
            model,
            input,
            tools = new object[]
            {
                new
                {
                    type = ResponsesImageTool,
                    size = DefaultGenerationSize
                }
            }
        };

        return new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");
    }

    private static HttpRequestMessage BuildResponsesRequest(
        string endpoint,
        IReadOnlyList<string> imagePaths,
        string prompt,
        string model)
    {
        return new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = BuildResponsesContent(imagePaths, prompt, model)
        };
    }

    private static List<object> BuildResponsesInputContent(
        IReadOnlyList<string> imagePaths,
        string prompt)
    {
        var content = new List<object>
        {
            new
            {
                type = "input_text",
                text = prompt
            }
        };

        foreach (var imagePath in imagePaths)
        {
            var (bytes, mimeType) = BuildResponsesReferenceImagePayload(imagePath);
            var base64 = Convert.ToBase64String(bytes);
            content.Add(new
            {
                type = "input_image",
                image_url = $"data:{mimeType};base64,{base64}"
            });
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
        var statusCodeValue = (int)statusCode;
        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new InvalidOperationException($"AI 服务鉴权失败：{detail}"),
            HttpStatusCode.Forbidden => new InvalidOperationException($"AI 服务拒绝访问：{detail}"),
            HttpStatusCode.TooManyRequests => new InvalidOperationException($"AI 服务当前限流或余额不足：{detail}"),
            HttpStatusCode.RequestTimeout => new TimeoutException($"AI 服务请求超时：{detail}"),
            _ when statusCodeValue == 524 => new TimeoutException($"AI 服务等待上游响应超时（524）：{detail}"),
            _ when statusCodeValue is 502 or 503 or 504 or 522 => new HttpRequestException($"AI 服务网关暂时不可用（{statusCodeValue}）：{detail}"),
            _ => new InvalidOperationException($"AI 服务返回错误：{detail}")
        };
    }

    private static HttpClient CreateHttpClient(
        AiGenerationPreferences preferences,
        string apiKey,
        Func<HttpMessageHandler?>? httpMessageHandlerFactory = null)
    {
        var handler = httpMessageHandlerFactory?.Invoke();
        var client = handler == null
            ? new HttpClient()
            : new HttpClient(handler, disposeHandler: true);
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(preferences.TimeoutSeconds, 30, 300));
        client.BaseAddress = new Uri(preferences.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
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

    // Responses 路由会把 data URL 计入输入体积，先缩小参考图，避免中转把大图 base64 后拖到超时。
    private static (byte[] Bytes, string MimeType) BuildResponsesReferenceImagePayload(string imagePath)
    {
        using var image = Image.Load<Rgba32>(imagePath);
        if (image.Width <= MaxResponsesReferenceImageSide && image.Height <= MaxResponsesReferenceImageSide)
        {
            return (File.ReadAllBytes(imagePath), ResolveImageMimeType(imagePath));
        }

        using var clone = image.Clone();
        clone.Mutate(operation => operation.Resize(new ResizeOptions
        {
            Size = new Size(MaxResponsesReferenceImageSide, MaxResponsesReferenceImageSide),
            Mode = ResizeMode.Max
        }));

        using var stream = new MemoryStream();
        clone.SaveAsJpeg(stream, new JpegEncoder
        {
            Quality = ResponsesReferenceImageQuality
        });

        return (stream.ToArray(), "image/jpeg");
    }

    private static bool ShouldUseResponsesApi(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return false;

        var normalized = model.Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        if (normalized.StartsWith("gptimage", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    private static bool ShouldUseImageGenerationApi(string model)
    {
        if (string.IsNullOrWhiteSpace(model))
            return false;

        return string.Equals(model.Trim(), "gpt-image-2", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<string> BuildReferenceImages(PersonalProfile profile, bool requireAvatar)
    {
        if (string.IsNullOrWhiteSpace(profile.AvatarPhotoPath))
        {
            if (requireAvatar)
                throw new InvalidOperationException("生成效果图至少需要一张头像照。");

            return [];
        }

        var referenceImages = new List<string>
        {
            _assetStorageService.GetProfileReferenceFullPath(profile.AvatarPhotoPath)
        };

        if (!string.IsNullOrWhiteSpace(profile.FullBodyPhotoPath))
        {
            referenceImages.Add(_assetStorageService.GetProfileReferenceFullPath(profile.FullBodyPhotoPath));
        }

        return referenceImages;
    }

    private static string? TryExtractImageBase64FromImageEdits(string body)
    {
        using var document = JsonDocument.Parse(body);
        return document.RootElement
            .GetProperty("data")[0]
            .TryGetProperty("b64_json", out var base64Node)
            ? base64Node.GetString()
            : null;
    }

    private static string? TryExtractImageBase64FromResponses(string body)
    {
        using var document = JsonDocument.Parse(body);

        if (document.RootElement.TryGetProperty("output", out var output) &&
            output.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in output.EnumerateArray())
            {
                if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (!contentItem.TryGetProperty("type", out var typeNode))
                        continue;

                    var type = typeNode.GetString();
                    if (!string.Equals(type, "output_image", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(type, "image_generation_call", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (contentItem.TryGetProperty("image_base64", out var imageBase64))
                        return imageBase64.GetString();

                    if (contentItem.TryGetProperty("result", out var result) &&
                        result.TryGetProperty("image_base64", out var resultImageBase64))
                    {
                        return resultImageBase64.GetString();
                    }
                }
            }
        }

        return null;
    }

    private static string BuildEndpoint(HttpClient client, string relativePath)
    {
        var baseUri = client.BaseAddress?.ToString()?.TrimEnd('/') ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUri))
            return "/" + relativePath.TrimStart('/');

        return baseUri.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? $"{baseUri}/{relativePath.TrimStart('/')}"
            : $"{baseUri}/v1/{relativePath.TrimStart('/')}";
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

    private static async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<HttpRequestMessage> requestFactory,
        Func<HttpRequestMessage, Task<HttpResponseMessage>> sendAsync,
        int retryCount)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            using var request = requestFactory();

            try
            {
                var response = await sendAsync(request);
                if (!ShouldRetry(response.StatusCode) || attempt == retryCount)
                    return response;

                response.Dispose();
            }
            catch (Exception ex) when (IsTransientException(ex) && attempt < retryCount)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromSeconds(1.2 * (attempt + 1)));
        }

        throw lastException ?? new InvalidOperationException("AI 请求重试失败。");
    }

    private static bool IsTransientException(Exception ex)
    {
        if (ex is TaskCanceledException || ex is TimeoutException)
            return true;

        if (ex is HttpRequestException httpRequestException)
        {
            var message = httpRequestException.Message;
            return message.Contains("524", StringComparison.OrdinalIgnoreCase)
                || message.Contains("522", StringComparison.OrdinalIgnoreCase)
                || message.Contains("503", StringComparison.OrdinalIgnoreCase)
                || message.Contains("504", StringComparison.OrdinalIgnoreCase)
                || message.Contains("timeout", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static bool ShouldRetry(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return statusCode == HttpStatusCode.RequestTimeout
            || statusCode == HttpStatusCode.TooManyRequests
            || statusCode == HttpStatusCode.BadGateway
            || statusCode == HttpStatusCode.ServiceUnavailable
            || statusCode == HttpStatusCode.GatewayTimeout
            || code is 522 or 524;
    }
}
