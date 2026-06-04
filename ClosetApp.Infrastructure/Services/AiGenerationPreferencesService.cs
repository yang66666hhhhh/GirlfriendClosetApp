using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class AiGenerationPreferencesService : IAiGenerationPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public AiGenerationPreferencesService(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "ai-generation-settings.json");
    }

    public async Task<AiGenerationPreferences> GetAsync()
    {
        if (!File.Exists(_filePath))
            return BuildDefault();

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var document = await JsonSerializer.DeserializeAsync<AiGenerationPreferencesDocument>(stream, JsonOptions);
            if (document == null)
                return BuildDefault();

            return new AiGenerationPreferences(
                document.BaseUrl ?? BuildDefault().BaseUrl,
                document.Model ?? BuildDefault().Model,
                document.TimeoutSeconds > 0 ? document.TimeoutSeconds : 60,
                document.LastConnectionCheckAt,
                !string.IsNullOrWhiteSpace(document.EncryptedApiKey));
        }
        catch
        {
            return BuildDefault();
        }
    }

    public async Task SaveAsync(SaveAiGenerationPreferencesRequest request)
    {
        var current = await LoadDocumentAsync();
        current.BaseUrl = string.IsNullOrWhiteSpace(request.BaseUrl) ? BuildDefault().BaseUrl : request.BaseUrl.Trim();
        current.Model = string.IsNullOrWhiteSpace(request.Model) ? BuildDefault().Model : request.Model.Trim();
        current.TimeoutSeconds = request.TimeoutSeconds <= 0 ? 60 : request.TimeoutSeconds;

        if (request.ClearApiKey)
        {
            current.EncryptedApiKey = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.ApiKey))
        {
            current.EncryptedApiKey = Protect(request.ApiKey.Trim());
        }

        await SaveDocumentAsync(current);
    }

    public async Task<string?> GetApiKeyAsync()
    {
        var current = await LoadDocumentAsync();
        return string.IsNullOrWhiteSpace(current.EncryptedApiKey)
            ? null
            : Unprotect(current.EncryptedApiKey);
    }

    public async Task MarkConnectionCheckedAsync(DateTime checkedAt)
    {
        var current = await LoadDocumentAsync();
        current.LastConnectionCheckAt = checkedAt;
        await SaveDocumentAsync(current);
    }

    private async Task<AiGenerationPreferencesDocument> LoadDocumentAsync()
    {
        if (!File.Exists(_filePath))
            return new AiGenerationPreferencesDocument
            {
                BaseUrl = BuildDefault().BaseUrl,
                Model = BuildDefault().Model,
                TimeoutSeconds = BuildDefault().TimeoutSeconds
            };

        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<AiGenerationPreferencesDocument>(stream, JsonOptions)
            ?? new AiGenerationPreferencesDocument
            {
                BaseUrl = BuildDefault().BaseUrl,
                Model = BuildDefault().Model,
                TimeoutSeconds = BuildDefault().TimeoutSeconds
            };
    }

    private async Task SaveDocumentAsync(AiGenerationPreferencesDocument document)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions);
    }

    private static AiGenerationPreferences BuildDefault()
    {
        return new AiGenerationPreferences(
            "https://api.openai.com/v1",
            "gpt-image-1",
            60);
    }

    private static string Protect(string value)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("AI API Key 的 DPAPI 加密当前仅支持 Windows。");

        var bytes = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string Unprotect(string value)
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("AI API Key 的 DPAPI 解密当前仅支持 Windows。");

        var bytes = Convert.FromBase64String(value);
        var unprotectedBytes = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(unprotectedBytes);
    }

    private sealed class AiGenerationPreferencesDocument
    {
        public string? BaseUrl { get; set; }
        public string? Model { get; set; }
        public int TimeoutSeconds { get; set; }
        public DateTime? LastConnectionCheckAt { get; set; }
        public string? EncryptedApiKey { get; set; }
    }
}
