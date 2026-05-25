using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IBackupService _backupService;
    private readonly IImageMaintenanceService _imageMaintenanceService;
    private readonly IWeatherService _weatherService;
    private readonly IWeatherPreferencesService _weatherPreferencesService;
    private readonly ThemeService _themeService;

    [ObservableProperty]
    private string _dataDir = AppPaths.BaseDir;

    [ObservableProperty]
    private string _imagesDir = AppPaths.ImagesDir;

    [ObservableProperty]
    private string _logDir = AppPaths.LogsDir;

    [ObservableProperty]
    private string _version = GetVersion();

    [ObservableProperty]
    private string _weatherCity = "Shanghai";

    [ObservableProperty]
    private AppThemeKind _currentTheme = AppThemeKind.Rose;

    [ObservableProperty]
    private string _themeSummary = "当前使用柔粉主题";

    [ObservableProperty]
    private string _themeDescription = "柔粉更柔和、沉稳，能保留生活感，也不会抢照片和衣物本身的视觉重点。";

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

    private bool _isRefreshingWeather;

    public SettingsViewModel(
        IBackupService backupService,
        IImageMaintenanceService imageMaintenanceService,
        IWeatherService weatherService,
        IWeatherPreferencesService weatherPreferencesService,
        ThemeService themeService)
    {
        _backupService = backupService;
        _imageMaintenanceService = imageMaintenanceService;
        _weatherService = weatherService;
        _weatherPreferencesService = weatherPreferencesService;
        _themeService = themeService;
    }

    public AppThemeKind CurrentThemeValue => _themeService.CurrentTheme;

    public async Task InitializeAsync()
    {
        await LoadWeatherPreferencesAsync();
        CurrentTheme = _themeService.CurrentTheme;
        UpdateThemeText();
    }

    public async Task ApplyThemeAsync(AppThemeKind theme)
    {
        await _themeService.ApplyThemeAsync(theme);
        CurrentTheme = theme;
        UpdateThemeText();
    }

    private void UpdateThemeText()
    {
        var isRose = CurrentTheme == AppThemeKind.Rose;
        ThemeSummary = isRose ? "当前使用柔粉主题" : "当前使用清蓝主题";
        ThemeDescription = isRose
            ? "柔粉更柔和、沉稳，能保留生活感，也不会抢照片和衣物本身的视觉重点。"
            : "清蓝更克制、清爽，页面会更冷静，也更偏中性工具感。";
    }

    private async Task LoadWeatherPreferencesAsync()
    {
        var preferences = await _weatherPreferencesService.GetAsync();
        WeatherCity = preferences.DefaultCity;
    }

    public async Task SaveWeatherCityAsync(string city)
    {
        await _weatherPreferencesService.SaveAsync(new WeatherPreferences
        {
            DefaultCity = city
        });
        WeatherCity = city;
    }

    public async Task RefreshWeatherAsync(bool showStatus)
    {
        if (_isRefreshingWeather)
            return;

        _isRefreshingWeather = true;
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
            var weather = await _weatherService.GetCurrentWeatherAsync(WeatherCity);
            if (weather == null)
            {
                WeatherSummary = "暂时没有可用天气。";
                WeatherDetails = $"没有找到“{WeatherCity}”的天气数据，请试试中文全名、英文城市名，或带上省/州名。";
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
        }
    }

    private static string BuildTimezoneSuffix(string timezone)
    {
        return string.IsNullOrWhiteSpace(timezone) ? string.Empty : $" · {timezone}";
    }

    private static string GetVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "开发版" : $"{version.Major}.{version.Minor}.{version.Build}";
    }
}
