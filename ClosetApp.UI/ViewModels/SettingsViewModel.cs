using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly IImageMaintenanceService _imageMaintenanceService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
    private readonly IRecommendationPreferencesService _recommendationPreferencesService;
    private readonly IAiGenerationPreferencesService? _aiGenerationPreferencesService;
    private readonly IPersonalProfileService? _personalProfileService;
    private readonly IAiImageGenerationService? _aiImageGenerationService;
    private readonly ThemeService _themeService;
    private readonly OutfitDisplayPreferencesService _outfitDisplayPreferencesService;

    [ObservableProperty]
    private string _weatherCity = "Shanghai";

    [ObservableProperty]
    private IReadOnlyList<WeatherCitySuggestion> _weatherCitySuggestions = [];

    [ObservableProperty]
    private bool _isWeatherCitySuggestionOpen;

    [ObservableProperty]
    private AppThemeKind _currentTheme = AppThemeKind.Rose;

    [ObservableProperty]
    private string _themeSummary = "当前使用柔粉主题";

    [ObservableProperty]
    private string _themeDescription = "柔粉更柔和、沉稳，能保留生活感，也不会抢照片和衣物本身的视觉重点。";

    [ObservableProperty]
    private AppFontSizeLevel _fontSizeLevel = AppFontSizeLevel.Standard;

    [ObservableProperty]
    private string _fontSizeSummary = "字体大小：标准";

    [ObservableProperty]
    private string _fontSizeDetail = "标准字号适合大多数桌面窗口。";

    [ObservableProperty]
    private OutfitCardDisplayMode _defaultOutfitCardDisplayMode = OutfitCardDisplayMode.OutfitFirst;

    [ObservableProperty]
    private string _outfitCardDisplaySummary = "默认展示：搭配卡片";

    [ObservableProperty]
    private string _outfitCardDisplayDetail = "搭配列表会优先展示原始搭配预览；没有效果图管理压力时会更稳。";

    [ObservableProperty]
    private string _imageStats = "";

    [ObservableProperty]
    private string _cacheStats = "";

    [ObservableProperty]
    private string _logStats = "";

    [ObservableProperty]
    private string _missingImageStats = "";

    [ObservableProperty]
    private string _thumbnailHealthStats = "";

    [ObservableProperty]
    private string _orphanOriginalStats = "";

    [ObservableProperty]
    private string _weatherSummary = "还没有获取天气。";

    [ObservableProperty]
    private string _weatherDetails = "保存城市后，点一次刷新就可以验证天气接口是否接通。";

    [ObservableProperty]
    private string _weatherObservedAt = "";

    [ObservableProperty]
    private string _weatherStatus = "";

    [ObservableProperty]
    private bool _isWeatherStatusVisible;

    [ObservableProperty]
    private OutfitScene? _recommendationDefaultScene;

    [ObservableProperty]
    private bool _recommendationAvoidWornToday = true;

    [ObservableProperty]
    private RecommendationRotationStrategy _recommendationRotationStrategy = RecommendationRotationStrategy.Balanced;

    [ObservableProperty]
    private string _recommendationStatus = "";

    [ObservableProperty]
    private bool _isRecommendationStatusVisible;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditWeather))]
    [NotifyPropertyChangedFor(nameof(WeatherSaveButtonText))]
    [NotifyPropertyChangedFor(nameof(WeatherRefreshButtonText))]
    private bool _isWeatherBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEditRecommendationPreferences))]
    [NotifyPropertyChangedFor(nameof(RecommendationSaveButtonText))]
    private bool _isRecommendationBusy;

    [ObservableProperty]
    private string _backupValidation = "";

    [ObservableProperty]
    private string _backupValidationData = "";

    [ObservableProperty]
    private string _backupValidationImages = "";

    [ObservableProperty]
    private string _backupValidationHint = "";

    [ObservableProperty]
    private string _backupValidationWarnings = "";

    [ObservableProperty]
    private bool _isBackupValidationWarningVisible;

    [ObservableProperty]
    private string _lastImportSummary = "还没有导入记录。";

    [ObservableProperty]
    private string _lastImportDetail = "导入完成后，这里会显示恢复结果和后续建议。";

    [ObservableProperty]
    private string _lastImportWarning = "";

    [ObservableProperty]
    private string _lastImportMissingFiles = "";

    [ObservableProperty]
    private bool _isLastImportWarningVisible;

    [ObservableProperty]
    private bool _isLastImportMissingCardVisible;

    [ObservableProperty]
    private bool _isRepairAfterImportVisible;

    [ObservableProperty]
    private string _backupHistoryEmptyText = "还没有备份记录。";

    [ObservableProperty]
    private bool _isBackupHistoryEmpty = true;

    [ObservableProperty]
    private IReadOnlyList<BackupHistoryItem> _backupHistory = [];

    [ObservableProperty]
    private string _aiBaseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _aiModel = "gpt-image-2";

    [ObservableProperty]
    private string _aiTimeoutSeconds = "60";

    [ObservableProperty]
    private string _aiSettingsSummary = "还没有完成 AI 图片生成配置。";

    [ObservableProperty]
    private string _aiSettingsDetail = "先保存 Base URL、模型和 API Key，再补齐头像、全身照和云端同意。";

    // 避免初始化或程序性回填城市名时，把状态条误显示成“待保存”。
    private bool _suppressWeatherCityPrompt;
    private CancellationTokenSource? _weatherSuggestionCts;
    private bool _isRefreshingWeather;

    public SettingsViewModel(
        IBackupService backupService,
        IImageMaintenanceService imageMaintenanceService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService,
        IRecommendationPreferencesService recommendationPreferencesService,
        ThemeService themeService,
        OutfitDisplayPreferencesService outfitDisplayPreferencesService,
        IAiGenerationPreferencesService? aiGenerationPreferencesService = null,
        IPersonalProfileService? personalProfileService = null,
        IAiImageGenerationService? aiImageGenerationService = null)
    {
        _backupService = backupService;
        _imageMaintenanceService = imageMaintenanceService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
        _recommendationPreferencesService = recommendationPreferencesService;
        _themeService = themeService;
        _outfitDisplayPreferencesService = outfitDisplayPreferencesService;
        _aiGenerationPreferencesService = aiGenerationPreferencesService;
        _personalProfileService = personalProfileService;
        _aiImageGenerationService = aiImageGenerationService;
        _outfitDisplayPreferencesService.PreferenceChanged += OutfitDisplayPreferencesService_PreferenceChanged;
    }

    public bool CanEditWeather => !IsWeatherBusy;
    public string WeatherSaveButtonText => IsWeatherBusy ? "处理中..." : "保存";
    public string WeatherRefreshButtonText => IsWeatherBusy ? "刷新中..." : "刷新天气";
    public bool CanEditRecommendationPreferences => !IsRecommendationBusy;
    public string RecommendationSaveButtonText => IsRecommendationBusy ? "保存中..." : "保存推荐偏好";
    public string FontSizePreset => FontSizeLevel switch
    {
        AppFontSizeLevel.Small => "Compact",
        AppFontSizeLevel.Large or AppFontSizeLevel.ExtraLarge => "Expanded",
        _ => "Balanced"
    };
    public IReadOnlyList<OutfitSceneFilterOption> RecommendationSceneOptions { get; } =
    [
        new("不限场景", null),
        new("通勤", OutfitScene.Work),
        new("约会", OutfitScene.Date),
        new("出游", OutfitScene.Travel),
        new("派对", OutfitScene.Party),
        new("休闲", OutfitScene.Casual)
    ];
    public IReadOnlyList<RecommendationRotationStrategyOption> RecommendationRotationStrategyOptions { get; } =
    [
        new("均衡推荐", RecommendationRotationStrategy.Balanced),
        new("优先少穿", RecommendationRotationStrategy.PreferLessWorn),
        new("优先收藏", RecommendationRotationStrategy.PreferFavorites)
    ];
    public async Task InitializeAsync()
    {
        await LoadWeatherPreferencesAsync();
        await LoadRecommendationPreferencesAsync();
        await LoadOutfitDisplayPreferencesAsync();
        await RefreshAiGenerationSettingsAsync();
        CurrentTheme = _themeService.CurrentTheme;
        ApplyFontSizeLevel(_themeService.CurrentFontSizeLevel);
        UpdateThemeText();
    }

    public async Task RefreshAiGenerationSettingsAsync()
    {
        if (_aiGenerationPreferencesService == null || _personalProfileService == null)
        {
            AiSettingsSummary = "当前环境未启用 AI 图片生成配置。";
            AiSettingsDetail = "等服务接入后，这里会显示 provider 配置和个人档案准备度。";
            return;
        }

        var preferences = await _aiGenerationPreferencesService.GetAsync();
        var profile = await _personalProfileService.GetCurrentAsync();

        AiBaseUrl = preferences.BaseUrl;
        AiModel = preferences.Model;
        AiTimeoutSeconds = preferences.TimeoutSeconds.ToString();

        var providerReady =
            !string.IsNullOrWhiteSpace(preferences.BaseUrl) &&
            !string.IsNullOrWhiteSpace(preferences.Model) &&
            preferences.HasEncryptedApiKey;

        var profileReady =
            profile != null &&
            !string.IsNullOrWhiteSpace(profile.DisplayName) &&
            profile.HasMinimumReferencePhotos &&
            profile.HasConsent;

        AiSettingsSummary = providerReady && profileReady
            ? "AI 图片生成已准备好。"
            : "AI 图片生成还差一点准备。";

        var protocolHint = string.IsNullOrWhiteSpace(preferences.Model)
            ? "协议待定。"
            : string.Equals(preferences.Model, "gpt-image-2", StringComparison.OrdinalIgnoreCase)
                ? "当前会走 images 文生图接口，不上传参考图。"
            : preferences.Model.StartsWith("gpt-image-", StringComparison.OrdinalIgnoreCase)
                ? "当前会走 images 图片编辑接口。"
                : "当前会走 responses 图片生成接口；这类非图片模型在部分中转上更容易因为参考图输入过大或网关超时而失败。";

        var detailParts = new List<string>();
        detailParts.Add(providerReady
            ? $"当前使用 {preferences.Model} · 已保存 API Key。"
            : "还没有完成 provider 配置。");
        detailParts.Add(profileReady
            ? $"个人档案已完成：{profile!.DisplayName}。"
            : string.Equals(preferences.Model, "gpt-image-2", StringComparison.OrdinalIgnoreCase)
                ? "个人档案还缺少昵称或云端同意。"
                : "个人档案还缺少昵称、上半身参考照或云端同意。");

        if (preferences.LastConnectionCheckAt.HasValue)
            detailParts.Add($"最近一次接口连通性测试：{preferences.LastConnectionCheckAt:yyyy-MM-dd HH:mm}。");

        detailParts.Add(protocolHint);
        detailParts.Add($"当前超时 {preferences.TimeoutSeconds} 秒。");

        AiSettingsDetail = string.Join(" ", detailParts);
    }

    public async Task TestAiConnectionAsync()
    {
        if (_aiImageGenerationService == null || _aiGenerationPreferencesService == null)
            throw new InvalidOperationException("当前环境未启用 AI 图片生成服务。");

        await _aiImageGenerationService.TestConnectionAsync();
        await _aiGenerationPreferencesService.MarkConnectionCheckedAsync(DateTime.Now);
        await RefreshAiGenerationSettingsAsync();
    }

    public async Task ApplyThemeAsync(AppThemeKind theme)
    {
        await _themeService.ApplyThemeAsync(theme);
        CurrentTheme = theme;
        UpdateThemeText();
    }

    public async Task SaveFontSizeLevelAsync(AppFontSizeLevel level)
    {
        if (FontSizeLevel == level)
            return;

        await _themeService.ApplyFontSizeAsync(level);
        ApplyFontSizeLevel(level);
        ToastService.Instance.ShowSuccess("已调整字体大小", FontSizeSummary);
    }

    private void UpdateThemeText()
    {
        var isRose = CurrentTheme == AppThemeKind.Rose;
        ThemeSummary = isRose ? "当前使用柔粉主题" : "当前使用清蓝主题";
        ThemeDescription = isRose
            ? "柔粉更柔和、沉稳，能保留生活感，也不会抢照片和衣物本身的视觉重点。"
            : "清蓝更克制、清爽，页面会更冷静，也更偏中性工具感。";
    }

    private void ApplyFontSizeLevel(AppFontSizeLevel level)
    {
        FontSizeLevel = level;
        OnPropertyChanged(nameof(FontSizePreset));
        FontSizeSummary = $"字体大小：{GetFontSizeLevelLabel(level)}";
        FontSizeDetail = level switch
        {
            AppFontSizeLevel.Small => "更紧凑，适合希望一屏看到更多内容的窗口。",
            AppFontSizeLevel.Comfortable => "比标准略大，阅读更轻松，布局仍然克制。",
            AppFontSizeLevel.Large => "明显放大，适合长时间浏览衣柜和设置。",
            AppFontSizeLevel.ExtraLarge => "最大字号，优先保证可读性。",
            _ => "标准字号适合大多数桌面窗口。"
        };
    }

    public static string GetFontSizeLevelLabel(AppFontSizeLevel level) => level switch
    {
        AppFontSizeLevel.Small => "小",
        AppFontSizeLevel.Comfortable => "舒适",
        AppFontSizeLevel.Large => "大",
        AppFontSizeLevel.ExtraLarge => "特大",
        _ => "标准"
    };

    partial void OnWeatherCityChanged(string value)
    {
        if (_suppressWeatherCityPrompt || IsWeatherBusy || _isRefreshingWeather)
        {
            return;
        }

        var city = value.Trim();
        ShowWeatherStatus(string.IsNullOrWhiteSpace(city)
            ? "请先输入默认城市。"
            : "城市已更新，点击保存或刷新天气。");

        _ = RefreshWeatherCitySuggestionsAsync(city);
    }

    private async Task LoadWeatherPreferencesAsync()
    {
        var preferences = await _weatherPreferencesService.GetAsync();
        _suppressWeatherCityPrompt = true;
        try
        {
            WeatherCity = preferences.DefaultCity;
        }
        finally
        {
            _suppressWeatherCityPrompt = false;
        }
    }

    private async Task LoadRecommendationPreferencesAsync()
    {
        var preferences = await _recommendationPreferencesService.GetAsync();
        RecommendationDefaultScene = preferences.DefaultScene;
        RecommendationAvoidWornToday = preferences.AvoidWornToday;
        RecommendationRotationStrategy = preferences.RotationStrategy;
    }

    private async Task LoadOutfitDisplayPreferencesAsync()
    {
        var preferences = await _outfitDisplayPreferencesService.GetAsync();
        ApplyOutfitCardDisplayMode(preferences.DefaultCardDisplayMode);
    }

    public async Task SaveRecommendationPreferencesAsync()
    {
        if (IsRecommendationBusy)
            return;

        IsRecommendationBusy = true;
        RecommendationStatus = "正在保存今日推荐偏好...";
        IsRecommendationStatusVisible = true;

        try
        {
            await _recommendationPreferencesService.SaveAsync(new RecommendationPreferences
            {
                DefaultScene = RecommendationDefaultScene,
                AvoidWornToday = RecommendationAvoidWornToday,
                RotationStrategy = RecommendationRotationStrategy
            });

            RecommendationStatus = "今日推荐偏好已保存。";
            ToastService.Instance.ShowSuccess("已保存推荐偏好");
        }
        finally
        {
            IsRecommendationBusy = false;
        }
    }

    public async Task SaveOutfitCardDisplayModeAsync(OutfitCardDisplayMode mode)
    {
        if (DefaultOutfitCardDisplayMode == mode)
            return;

        await _outfitDisplayPreferencesService.SaveAsync(new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = mode
        });
    }

    public async Task SaveWeatherCityAsync(string city)
    {
        if (IsWeatherBusy)
            return;

        city = city.Trim();
        if (string.IsNullOrWhiteSpace(city))
        {
            ClearWeatherCitySuggestions();
            ShowWeatherStatus("请先输入默认城市。");
            return;
        }

        IsWeatherBusy = true;
        ShowWeatherStatus("正在保存默认城市...");

        try
        {
            await _weatherPreferencesService.SaveAsync(new WeatherPreferences
            {
                DefaultCity = city
            });
            ClearWeatherCitySuggestions();
            WeatherCity = city;
            WeatherSummary = $"默认城市已切换为 {city}";
            WeatherDetails = "点击刷新天气，更新当前城市的实时天气。";
            WeatherObservedAt = string.Empty;
            ShowWeatherStatus($"默认城市已保存为 {city}。");
            ToastService.Instance.ShowSuccess("已保存默认城市", city);
        }
        finally
        {
            IsWeatherBusy = false;
        }
    }

    public async Task RefreshWeatherAsync(bool showStatus)
    {
        if (_isRefreshingWeather)
            return;

        var city = WeatherCity.Trim();
        if (string.IsNullOrWhiteSpace(city))
        {
            ClearWeatherCitySuggestions();
            WeatherSummary = "暂时没有可用天气。";
            WeatherDetails = "请输入城市后再刷新。";
            WeatherObservedAt = string.Empty;
            ShowWeatherStatus("请输入城市后再刷新。");
            return;
        }

        _isRefreshingWeather = true;
        IsWeatherBusy = true;
        WeatherSummary = "正在获取天气...";
        WeatherDetails = "稍等一下，我正在请求实时天气。";
        WeatherObservedAt = string.Empty;

        if (showStatus)
        {
            WeatherStatus = "正在刷新天气...";
            IsWeatherStatusVisible = true;
        }

        try
        {
            await _weatherPreferencesService.SaveAsync(new WeatherPreferences
            {
                DefaultCity = city
            });
            ClearWeatherCitySuggestions();
            WeatherCity = city;

            var weather = await _weatherService.GetCurrentWeatherAsync(city);
            if (weather == null)
            {
                WeatherSummary = "暂时没有可用天气。";
                WeatherDetails = $"没有找到“{city}”的天气数据，请试试中文全名、英文城市名，或带上省/州名。";
                return;
            }

            WeatherSummary = $"{weather.City} · {weather.Temperature}°C · {weather.Condition}";
            WeatherDetails = $"湿度 {weather.Humidity}%{BuildTimezoneSuffix(weather.Timezone)}";
            WeatherObservedAt = weather.ObservedAt.HasValue
                ? $"观测时间 {weather.ObservedAt:yyyy-MM-dd HH:mm}"
                : string.Empty;

            if (showStatus)
            {
                WeatherStatus = $"已刷新 {weather.City} 的实时天气。";
                IsWeatherStatusVisible = true;
            }
        }
        catch (Exception ex)
        {
            WeatherSummary = "暂时没有可用天气。";
            WeatherDetails = $"天气刷新失败：{ex.Message}";
        }
        finally
        {
            _isRefreshingWeather = false;
            IsWeatherBusy = false;
        }
    }

    public void SelectWeatherCitySuggestion(WeatherCitySuggestion suggestion)
    {
        if (suggestion == null || string.IsNullOrWhiteSpace(suggestion.DisplayName))
            return;

        _suppressWeatherCityPrompt = true;
        try
        {
            WeatherCity = suggestion.DisplayName;
        }
        finally
        {
            _suppressWeatherCityPrompt = false;
        }

        HideWeatherCitySuggestions();
        ShowWeatherStatus("城市已选中，点击保存或刷新天气。");
    }

    public void HideWeatherCitySuggestions()
    {
        _weatherSuggestionCts?.Cancel();
        IsWeatherCitySuggestionOpen = false;
    }

    public void ClearWeatherCitySuggestions()
    {
        HideWeatherCitySuggestions();
        WeatherCitySuggestions = [];
    }

    private async Task RefreshWeatherCitySuggestionsAsync(string city)
    {
        _weatherSuggestionCts?.Cancel();

        if (string.IsNullOrWhiteSpace(city) || city.Length < 1)
        {
            ClearWeatherCitySuggestions();
            return;
        }

        var cts = new CancellationTokenSource();
        _weatherSuggestionCts = cts;

        try
        {
            await Task.Delay(180, cts.Token);
            var suggestions = await _weatherService.SearchCitiesAsync(city, maxResults: 6);
            if (cts.IsCancellationRequested)
                return;

            var exactMatch = suggestions.Any(item => string.Equals(item.DisplayName, city, StringComparison.OrdinalIgnoreCase));
            WeatherCitySuggestions = suggestions;
            IsWeatherCitySuggestionOpen = suggestions.Count > 0 && !exactMatch;
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            WeatherCitySuggestions = [];
            IsWeatherCitySuggestionOpen = false;
        }
        finally
        {
            if (ReferenceEquals(_weatherSuggestionCts, cts))
            {
                _weatherSuggestionCts = null;
            }

            cts.Dispose();
        }
    }

    private void ShowWeatherStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            WeatherStatus = string.Empty;
            IsWeatherStatusVisible = false;
            return;
        }

        WeatherStatus = message;
        IsWeatherStatusVisible = true;
    }

    public async Task RefreshStatsAsync()
    {
        var originalCount = await _imageMaintenanceService.CountFilesAsync(AppPaths.OriginalsDir);
        var originalSize = await _imageMaintenanceService.GetDirectorySizeAsync(AppPaths.OriginalsDir);
        var displayCount = await _imageMaintenanceService.CountFilesAsync(AppPaths.DisplayDir);
        var displaySize = await _imageMaintenanceService.GetDirectorySizeAsync(AppPaths.DisplayDir);
        var thumbnailCount = await _imageMaintenanceService.CountFilesAsync(AppPaths.ThumbnailsDir);
        var thumbnailSize = await _imageMaintenanceService.GetDirectorySizeAsync(AppPaths.ThumbnailsDir);
        var logCount = await _imageMaintenanceService.CountFilesAsync(AppPaths.LogsDir);
        var logSize = await _imageMaintenanceService.GetDirectorySizeAsync(AppPaths.LogsDir);
        var missingImageCount = await _imageMaintenanceService.CountMissingImagesAsync();
        var missingThumbnailCount = await _imageMaintenanceService.CountMissingThumbnailsAsync();
        var orphanOriginals = await _imageMaintenanceService.AnalyzeOrphanOriginalsAsync();

        ImageStats = $"{originalCount} 张原图 · {FileSizeFormatter.Format(originalSize)}";
        CacheStats = $"{displayCount} 个主视觉缓存 · {thumbnailCount} 个小预览缓存 · {FileSizeFormatter.Format(displaySize + thumbnailSize)}";
        ThumbnailHealthStats = BuildThumbnailHealthText(missingThumbnailCount);
        OrphanOriginalStats = BuildOrphanOriginalsText(orphanOriginals);
        LogStats = $"{logCount} 个日志文件 · {FileSizeFormatter.Format(logSize)}";
        MissingImageStats = missingImageCount == 0
            ? "没有发现缺失图片"
            : $"{missingImageCount} 件衣服的图片路径失效";
    }


    public static string BuildThumbnailHealthText(int missingThumbnailCount)
    {
        return missingThumbnailCount == 0
            ? "所有已存在的原图都已经生成主视觉和小预览缓存。"
            : $"{missingThumbnailCount} 张图片缺少主视觉或小预览缓存，可一键重建。";
    }

    public static string BuildOrphanOriginalsText(OrphanOriginalsResult result)
    {
        return result.HasOrphans
            ? $"{result.OrphanCount} 张原图未被数据库引用，占用 {FileSizeFormatter.Format(result.TotalBytes)}。"
            : "没有发现孤儿原图。";
    }

    public async Task<BackupValidationResult> RefreshBackupStateAsync(BackupImportResult? latestImport = null)
    {
        var previewPath = Path.Combine(AppPaths.BackupsDir, $"preview-{Guid.NewGuid():N}.zip");
        var validation = await _backupService.ValidateExportAsync(previewPath);
        ApplyBackupValidation(validation);

        BackupHistory = await _backupService.GetHistoryAsync();
        IsBackupHistoryEmpty = BackupHistory.Count == 0;
        BackupHistoryEmptyText = IsBackupHistoryEmpty ? "还没有备份记录。" : string.Empty;

        if (latestImport != null)
        {
            ApplyLatestImport(latestImport);
            return validation;
        }

        var latestImportHistory = BackupHistory.FirstOrDefault(item => item.Operation == "Import" && item.Success);
        if (latestImportHistory == null)
        {
            ResetLatestImport();
            return validation;
        }

        LastImportSummary = latestImportHistory.Summary;
        LastImportDetail = $"{latestImportHistory.TimestampText} · {latestImportHistory.FileName}";
        LastImportWarning = string.Empty;
        LastImportMissingFiles = string.Empty;
        IsLastImportWarningVisible = false;
        IsLastImportMissingCardVisible = false;
        IsRepairAfterImportVisible = false;
        return validation;
    }

    public string BuildDefaultBackupPath() => _backupService.BuildDefaultBackupPath();

    public Task<BackupValidationResult> ValidateBackupExportAsync(string filePath)
        => _backupService.ValidateExportAsync(filePath);

    public async Task<BackupExportResult> ExportBackupWithFeedbackAsync(string filePath)
    {
        var result = await _backupService.ExportAsync(filePath);
        await RefreshBackupStateAsync();
        ToastService.Instance.ShowSuccess("备份已导出", Path.GetFileName(result.FilePath));
        return result;
    }

    public async Task<BackupImportResult> ImportBackupWithFeedbackAsync(string filePath)
    {
        BackupImportResult result;
        try
        {
            result = await _backupService.ImportAsync(filePath);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("备份导入失败", ex.Message);
            throw;
        }

        if (result.Success)
            await RefreshStatsAsync();

        await RefreshBackupStateAsync(result);
        if (result.Success)
            ToastService.Instance.ShowSuccess("备份已导入", "衣柜、搭配和标签列表已经刷新。");
        return result;
    }

    public async Task ClearBackupHistoryWithFeedbackAsync()
    {
        await _backupService.ClearHistoryAsync();
        await RefreshBackupStateAsync();
        ToastService.Instance.ShowSuccess("备份历史已清空");
    }

    public static string BuildValidationHint(BackupValidationResult validation)
    {
        if (validation.IsEmptyBackup)
            return validation.ReadinessSummary;

        if (!validation.HasWarnings)
            return "当前可以直接导出 ZIP 备份包，建议优先使用 ZIP 保留图片。";

        return string.Join(" ", validation.Warnings);
    }

    private void ApplyBackupValidation(BackupValidationResult validation)
    {
        BackupValidation = validation.ReadinessSummary;
        BackupValidationData = validation.DataSummary;
        BackupValidationImages = validation.ImageSummary;
        BackupValidationHint = BuildValidationHint(validation);
        IsBackupValidationWarningVisible = validation.HasWarnings;
        BackupValidationWarnings = validation.HasWarnings ? string.Join("\n", validation.Warnings) : string.Empty;
    }

    private void ApplyLatestImport(BackupImportResult result)
    {
        LastImportSummary = result.Summary;
        LastImportDetail = result.Success
            ? $"{result.ImportedAt:yyyy-MM-dd HH:mm} · {Path.GetFileName(result.FilePath)}\n" +
              $"衣服 {result.ClothingCount} · 搭配 {result.OutfitCount} · 标签 {result.TagCount} · 恢复图片 {result.RestoredImageCount}"
            : $"{result.ImportedAt:yyyy-MM-dd HH:mm} · {Path.GetFileName(result.FilePath)}\n" +
              $"导入阶段：{result.FailureStage ?? "导入"} · 数据库已回滚：{(result.DatabaseRolledBack ? "是" : "否")} · 恢复图片 {result.RestoredImageCount}";

        IsLastImportWarningVisible = result.Warnings.Count > 0 || !result.Success;
        LastImportWarning = !result.Success
            ? string.Join(" ", result.Warnings.Append(result.FailureDetail ?? string.Empty).Where(text => !string.IsNullOrWhiteSpace(text)))
            : result.Warnings.Count > 0 ? string.Join(" ", result.Warnings) : string.Empty;
        IsRepairAfterImportVisible = result.Success && result.ShouldSuggestRepair && result.Warnings.Count > 0;

        IsLastImportMissingCardVisible = result.MissingImageFiles.Count > 0;
        LastImportMissingFiles = result.MissingImageFiles.Count == 0
            ? string.Empty
            : string.Join("、", result.MissingImageFiles.Take(6)) +
                (result.MissingImageFiles.Count > 6 ? $" 等 {result.MissingImageFiles.Count} 个文件" : string.Empty);
    }

    private void ResetLatestImport()
    {
        LastImportSummary = "还没有导入记录。";
        LastImportDetail = "导入完成后，这里会显示恢复结果和后续建议。";
        LastImportWarning = string.Empty;
        LastImportMissingFiles = string.Empty;
        IsLastImportWarningVisible = false;
        IsLastImportMissingCardVisible = false;
        IsRepairAfterImportVisible = false;
    }

    private static string BuildTimezoneSuffix(string timezone)
    {
        return string.IsNullOrWhiteSpace(timezone) ? string.Empty : $" · {timezone}";
    }

    private void ApplyOutfitCardDisplayMode(OutfitCardDisplayMode mode)
    {
        DefaultOutfitCardDisplayMode = mode;
        OutfitCardDisplaySummary = mode == OutfitCardDisplayMode.EffectImageFirst
            ? "默认展示：效果图卡片"
            : "默认展示：搭配卡片";
        OutfitCardDisplayDetail = mode == OutfitCardDisplayMode.EffectImageFirst
            ? "首页会优先显示你保存的效果图，没有效果图时会自动回退到原始搭配。"
            : "首页会优先显示原始搭配卡片，适合先看穿搭结构。";
    }

    private void OutfitDisplayPreferencesService_PreferenceChanged(object? sender, OutfitDisplayPreferencesChangedEventArgs e)
    {
        ApplyOutfitCardDisplayMode(e.Preferences.DefaultCardDisplayMode);
    }
}

public sealed record RecommendationRotationStrategyOption(string Label, RecommendationRotationStrategy Value);
