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
    private readonly UserScopedSettingsPath _settingsPath;

    public AiGenerationPreferencesService(string? filePath = null, ICurrentUserContext? currentUserContext = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "ai-generation-settings.json");
        _settingsPath = new UserScopedSettingsPath(currentUserContext, _filePath);
    }

    public async Task<AiGenerationPreferences> GetAsync()
    {
        await _settingsPath.MigrateGlobalFileIfNeededAsync();
        var path = await _settingsPath.ResolveAsync();
        if (!File.Exists(path))
            return BuildDefault();

        try
        {
            var document = await LoadDocumentAsync();
            return ToPreferences(document);
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
        await _settingsPath.MigrateGlobalFileIfNeededAsync();
        var path = await _settingsPath.ResolveAsync();
        if (!File.Exists(path))
            return CreateDefaultDocument();

        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var document = await JsonSerializer.DeserializeAsync<AiGenerationPreferencesDocument>(stream, JsonOptions);
        return NormalizeDocument(document);
    }

    private async Task SaveDocumentAsync(AiGenerationPreferencesDocument document)
    {
        var path = await _settingsPath.ResolveAsync();
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, document, JsonOptions);
    }

    private static AiGenerationPreferences BuildDefault()
    {
        return new AiGenerationPreferences(
            "https://api.openai.com/v1",
            "gpt-image-2",
            180);
    }

    private static AiGenerationPreferencesDocument CreateDefaultDocument()
    {
        var defaults = BuildDefault();
        return new AiGenerationPreferencesDocument
        {
            BaseUrl = defaults.BaseUrl,
            Model = defaults.Model,
            TimeoutSeconds = defaults.TimeoutSeconds
        };
    }

    private static AiGenerationPreferencesDocument NormalizeDocument(AiGenerationPreferencesDocument? document)
    {
        var defaults = BuildDefault();
        return new AiGenerationPreferencesDocument
        {
            BaseUrl = string.IsNullOrWhiteSpace(document?.BaseUrl) ? defaults.BaseUrl : document.BaseUrl.Trim(),
            Model = string.IsNullOrWhiteSpace(document?.Model) ? defaults.Model : document.Model.Trim(),
            TimeoutSeconds = document?.TimeoutSeconds > 0 ? document.TimeoutSeconds : defaults.TimeoutSeconds,
            LastConnectionCheckAt = document?.LastConnectionCheckAt,
            EncryptedApiKey = document?.EncryptedApiKey
        };
    }

    private static AiGenerationPreferences ToPreferences(AiGenerationPreferencesDocument document)
    {
        return new AiGenerationPreferences(
            document.BaseUrl ?? BuildDefault().BaseUrl,
            document.Model ?? BuildDefault().Model,
            document.TimeoutSeconds > 0 ? document.TimeoutSeconds : 60,
            document.LastConnectionCheckAt,
            !string.IsNullOrWhiteSpace(document.EncryptedApiKey));
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
