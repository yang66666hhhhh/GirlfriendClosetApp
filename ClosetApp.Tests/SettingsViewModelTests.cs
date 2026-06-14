using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsViewModelTests
{
    [Fact]
    public async Task RefreshStatsAsync_UpdatesImageMaintenanceSummaries()
    {
        var imageMaintenance = new FakeImageMaintenanceService
        {
            MissingImages = 2,
            MissingThumbnails = 3,
            OrphanOriginals = new OrphanOriginalsResult(4, 2048)
        };
        var viewModel = CreateViewModel(imageMaintenance);

        await viewModel.RefreshStatsAsync();

        Assert.Contains("原图", viewModel.ImageStats);
        Assert.Contains("主视觉缓存", viewModel.CacheStats);
        Assert.Equal("2 件衣服的图片路径失效", viewModel.MissingImageStats);
        Assert.Equal("3 张图片缺少主视觉或小预览缓存，可一键重建。", viewModel.ThumbnailHealthStats);
        Assert.Equal("4 张原图未被数据库引用，占用 2 KB。", viewModel.OrphanOriginalStats);
    }

    [Fact]
    public void FormatSize_UsesReadableUnits()
    {
        Assert.Equal("512 B", FileSizeFormatter.Format(512));
        Assert.Equal("2 KB", FileSizeFormatter.Format(2048));
        Assert.Equal("1.5 MB", FileSizeFormatter.Format(1572864));
    }

    [Fact]
    public async Task RefreshBackupStateAsync_UpdatesValidationHistoryAndLatestImport()
    {
        var backup = new FakeBackupService
        {
            Validation = CreateValidation(warnings: ["有 1 张图片缺失。"]),
            History =
            [
                new BackupHistoryItem(
                    new DateTime(2026, 5, 28, 9, 30, 0),
                    "Import",
                    "zip",
                    @"D:\backup\closet.zip",
                    4096,
                    true,
                    "导入 2 件衣服、1 套搭配、3 个标签，恢复 2 张图片。")
            ]
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), backup);

        await viewModel.RefreshBackupStateAsync();

        Assert.Equal("导出前建议先看下面的提醒，确认后再继续。", viewModel.BackupValidation);
        Assert.Equal("有 1 张图片缺失。", viewModel.BackupValidationWarnings);
        Assert.True(viewModel.IsBackupValidationWarningVisible);
        Assert.Single(viewModel.BackupHistory);
        Assert.False(viewModel.IsBackupHistoryEmpty);
        Assert.Contains("closet.zip", viewModel.LastImportDetail);
        Assert.False(viewModel.IsLastImportWarningVisible);
    }

    [Fact]
    public async Task RefreshBackupStateAsync_WithLatestImport_UpdatesWarningAndMissingCards()
    {
        var backup = new FakeBackupService
        {
            Validation = CreateValidation(),
            History = []
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), backup);
        var latestImport = new BackupImportResult(
            @"D:\backup\latest.zip",
            "zip",
            new DateTime(2026, 5, 28, 10, 0, 0),
            2,
            1,
            3,
            4,
            5,
            6,
            2,
            ["a.jpg", "b.jpg", "c.jpg", "d.jpg", "e.jpg", "f.jpg", "g.jpg"],
            ["有图片没有恢复。"]);

        await viewModel.RefreshBackupStateAsync(latestImport);

        Assert.Contains("导入 2 件衣服", viewModel.LastImportSummary);
        Assert.Contains("latest.zip", viewModel.LastImportDetail);
        Assert.True(viewModel.IsLastImportWarningVisible);
        Assert.True(viewModel.IsRepairAfterImportVisible);
        Assert.True(viewModel.IsLastImportMissingCardVisible);
        Assert.Contains("等 7 个文件", viewModel.LastImportMissingFiles);
    }

    [Fact]
    public async Task ExportBackupWithFeedbackAsync_ExportsAndRefreshesBackupHistory()
    {
        var backup = new FakeBackupService
        {
            ExportResult = CreateExportResult(@"D:\backup\export.zip"),
            HistoryAfterExport =
            [
                new BackupHistoryItem(
                    new DateTime(2026, 5, 28, 11, 0, 0),
                    "Export",
                    "zip",
                    @"D:\backup\export.zip",
                    1024,
                    true,
                    "导出 2 件衣服、1 套搭配、3 个标签，打包 5 张图片。")
            ]
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), backup);

        var result = await viewModel.ExportBackupWithFeedbackAsync(@"D:\backup\export.zip");

        Assert.Equal(@"D:\backup\export.zip", backup.ExportedPath);
        Assert.Equal(result, backup.ExportResult);
        Assert.Single(viewModel.BackupHistory);
        Assert.False(viewModel.IsBackupHistoryEmpty);
    }

    [Fact]
    public async Task ImportBackupWithFeedbackAsync_ImportsRefreshesStatsAndLatestImport()
    {
        var imageMaintenance = new FakeImageMaintenanceService
        {
            MissingImages = 0,
            MissingThumbnails = 0,
            OrphanOriginals = new OrphanOriginalsResult(0, 0)
        };
        var backup = new FakeBackupService
        {
            ImportResult = CreateImportResult(@"D:\backup\import.zip")
        };
        var viewModel = CreateViewModel(imageMaintenance, backup);

        var result = await viewModel.ImportBackupWithFeedbackAsync(@"D:\backup\import.zip");

        Assert.Equal(@"D:\backup\import.zip", backup.ImportedPath);
        Assert.Equal(result, backup.ImportResult);
        Assert.Contains("导入 2 件衣服", viewModel.LastImportSummary);
        Assert.Equal("没有发现缺失图片", viewModel.MissingImageStats);
    }

    [Fact]
    public async Task RefreshBackupStateAsync_WithFailedLatestImport_ShowsRollbackContext()
    {
        var backup = new FakeBackupService
        {
            Validation = CreateValidation(),
            History = []
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), backup);
        var failedImport = new BackupImportResult(
            @"D:\backup\failed.zip",
            "zip",
            new DateTime(2026, 5, 28, 13, 0, 0),
            0,
            0,
            0,
            0,
            0,
            2,
            1,
            ["missing-a.jpg"],
            ["本次导入没有完成，数据库改动已回滚。"],
            Success: false,
            DatabaseRolledBack: true,
            FailureStage: "导入并恢复图片",
            FailureDetail: "测试异常");

        await viewModel.RefreshBackupStateAsync(failedImport);

        Assert.Contains("导入未完成", viewModel.LastImportSummary);
        Assert.Contains("数据库已回滚：是", viewModel.LastImportDetail);
        Assert.True(viewModel.IsLastImportWarningVisible);
        Assert.Contains("测试异常", viewModel.LastImportWarning);
        Assert.False(viewModel.IsRepairAfterImportVisible);
    }

    [Fact]
    public async Task ClearBackupHistoryWithFeedbackAsync_ClearsHistoryAndRefreshesEmptyState()
    {
        var backup = new FakeBackupService
        {
            History =
            [
                new BackupHistoryItem(DateTime.Now, "Export", "zip", @"D:\backup\old.zip", 1024, true, "导出完成")
            ],
            HistoryAfterClear = []
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), backup);
        await viewModel.RefreshBackupStateAsync();

        await viewModel.ClearBackupHistoryWithFeedbackAsync();

        Assert.True(backup.ClearHistoryCalled);
        Assert.Empty(viewModel.BackupHistory);
        Assert.True(viewModel.IsBackupHistoryEmpty);
    }

    [Fact]
    public async Task SaveRecommendationPreferencesAsync_PersistsRecommendationPreferences()
    {
        var recommendationPreferences = new FakeRecommendationPreferencesService();
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), recommendationPreferences: recommendationPreferences);
        viewModel.RecommendationDefaultScene = OutfitScene.Work;
        viewModel.RecommendationAvoidWornToday = false;
        viewModel.RecommendationRotationStrategy = RecommendationRotationStrategy.PreferLessWorn;

        await viewModel.SaveRecommendationPreferencesAsync();

        Assert.Equal(OutfitScene.Work, recommendationPreferences.SavedPreferences.DefaultScene);
        Assert.False(recommendationPreferences.SavedPreferences.AvoidWornToday);
        Assert.Equal(RecommendationRotationStrategy.PreferLessWorn, recommendationPreferences.SavedPreferences.RotationStrategy);
        Assert.True(viewModel.IsRecommendationStatusVisible);
    }

    [Fact]
    public async Task SaveWeatherCityAsync_UpdatesWeatherSnapshotToPendingSelectedCity()
    {
        var viewModel = CreateViewModel(new FakeImageMaintenanceService());
        viewModel.WeatherSummary = "上海 · 上海市 · 中国 · 22°C · 阴";
        viewModel.WeatherDetails = "湿度 74% · Asia/Shanghai";
        viewModel.WeatherObservedAt = "观测时间 2026-06-13 20:00";

        await viewModel.SaveWeatherCityAsync("佛山市 · 广东 · 中国");

        Assert.Equal("默认城市已切换为 佛山市 · 广东 · 中国", viewModel.WeatherSummary);
        Assert.Equal("点击刷新天气，更新当前城市的实时天气。", viewModel.WeatherDetails);
        Assert.Equal(string.Empty, viewModel.WeatherObservedAt);
        Assert.Equal("默认城市已保存为 佛山市 · 广东 · 中国。", viewModel.WeatherStatus);
    }

    [Fact]
    public async Task SaveOutfitCardDisplayModeAsync_PersistsDisplayMode()
    {
        var displayPreferences = CreateDisplayPreferencesService(OutfitCardDisplayMode.OutfitFirst);
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), outfitDisplayPreferences: displayPreferences);

        await viewModel.SaveOutfitCardDisplayModeAsync(OutfitCardDisplayMode.EffectImageFirst);

        var saved = await displayPreferences.GetAsync();
        Assert.Equal(OutfitCardDisplayMode.EffectImageFirst, saved.DefaultCardDisplayMode);
        Assert.Equal("当前默认：效果图优先", viewModel.OutfitCardDisplaySummary);
    }

    [Fact]
    public void WeatherCityChanged_WithInput_ShowsPendingWeatherPrompt()
    {
        var viewModel = CreateViewModel(new FakeImageMaintenanceService());

        viewModel.WeatherCity = "foshan";

        Assert.True(viewModel.IsWeatherStatusVisible);
        Assert.Equal("城市已更新，点击保存或刷新天气。", viewModel.WeatherStatus);
    }

    [Fact]
    public async Task InitializeAsync_LoadsSavedWeatherCity_WithoutPendingPrompt()
    {
        var weatherPreferences = new FakeWeatherPreferencesService
        {
            CurrentPreferences = new WeatherPreferences
            {
                DefaultCity = "Guangzhou"
            }
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), weatherPreferences: weatherPreferences);

        await viewModel.InitializeAsync();

        Assert.Equal("Guangzhou", viewModel.WeatherCity);
        Assert.False(viewModel.IsWeatherStatusVisible);
        Assert.Equal(string.Empty, viewModel.WeatherStatus);
    }

    [Fact]
    public async Task WeatherCityChanged_WithQuery_LoadsSuggestions()
    {
        var weatherService = new FakeWeatherService
        {
            Suggestions =
            [
                new WeatherCitySuggestion("Shanghai · China"),
                new WeatherCitySuggestion("Shantou · Guangdong · China")
            ]
        };
        var viewModel = CreateViewModel(new FakeImageMaintenanceService(), weatherService: weatherService);

        viewModel.WeatherCity = "sha";
        await Task.Delay(260);

        Assert.Equal("sha", weatherService.LastSearchedQuery);
        Assert.True(viewModel.IsWeatherCitySuggestionOpen);
        Assert.Equal(2, viewModel.WeatherCitySuggestions.Count);
    }

    [Fact]
    public void SelectWeatherCitySuggestion_AppliesSuggestionAndClosesDropdown()
    {
        var viewModel = CreateViewModel(new FakeImageMaintenanceService());

        viewModel.SelectWeatherCitySuggestion(new WeatherCitySuggestion("Foshan · Guangdong · China"));

        Assert.Equal("Foshan · Guangdong · China", viewModel.WeatherCity);
        Assert.False(viewModel.IsWeatherCitySuggestionOpen);
        Assert.Equal("城市已选中，点击保存或刷新天气。", viewModel.WeatherStatus);
    }

    [Fact]
    public void SelectWeatherCitySuggestion_PreservesChosenDisplayText()
    {
        var viewModel = CreateViewModel(new FakeImageMaintenanceService());
        viewModel.WeatherCitySuggestions =
        [
            new WeatherCitySuggestion("佛山市 · 广东 · 中国"),
            new WeatherCitySuggestion("佛山 · 云南 · 中国")
        ];
        viewModel.IsWeatherCitySuggestionOpen = true;

        viewModel.WeatherCity = "foshan";
        viewModel.SelectWeatherCitySuggestion(new WeatherCitySuggestion("佛山市 · 广东 · 中国"));

        Assert.Equal("佛山市 · 广东 · 中国", viewModel.WeatherCity);
        Assert.False(viewModel.IsWeatherCitySuggestionOpen);
        Assert.Equal(2, viewModel.WeatherCitySuggestions.Count);
    }

    private static SettingsViewModel CreateViewModel(
        FakeImageMaintenanceService imageMaintenance,
        FakeBackupService? backup = null,
        FakeRecommendationPreferencesService? recommendationPreferences = null,
        OutfitDisplayPreferencesService? outfitDisplayPreferences = null,
        FakeWeatherPreferencesService? weatherPreferences = null,
        FakeWeatherService? weatherService = null)
    {
        return new SettingsViewModel(
            backup ?? new FakeBackupService(),
            imageMaintenance,
            weatherService ?? new FakeWeatherService(),
            weatherPreferences ?? new FakeWeatherPreferencesService(),
            recommendationPreferences ?? new FakeRecommendationPreferencesService(),
            new ThemeService(new ThemePreferencesService(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"))),
            outfitDisplayPreferences ?? CreateDisplayPreferencesService(OutfitCardDisplayMode.OutfitFirst));
    }

    private static BackupValidationResult CreateValidation(IReadOnlyList<string>? warnings = null)
    {
        return new BackupValidationResult(
            "zip",
            2,
            1,
            3,
            4,
            5,
            6,
            5,
            1,
            warnings ?? []);
    }

    private static BackupExportResult CreateExportResult(string filePath)
    {
        return new BackupExportResult(
            filePath,
            "zip",
            new DateTime(2026, 5, 28, 11, 0, 0),
            1024,
            2,
            1,
            3,
            4,
            5,
            6,
            0,
            []);
    }

    private static BackupImportResult CreateImportResult(string filePath)
    {
        return new BackupImportResult(
            filePath,
            "zip",
            new DateTime(2026, 5, 28, 12, 0, 0),
            2,
            1,
            3,
            4,
            5,
            6,
            0,
            [],
            []);
    }

    private sealed class FakeImageMaintenanceService : IImageMaintenanceService
    {
        public int MissingImages { get; set; }
        public int MissingThumbnails { get; set; }
        public OrphanOriginalsResult OrphanOriginals { get; set; } = new(0, 0);

        public Task<int> CountMissingImagesAsync() => Task.FromResult(MissingImages);
        public Task<int> CountMissingThumbnailsAsync() => Task.FromResult(MissingThumbnails);
        public Task<ThumbnailRebuildResult> RebuildMissingThumbnailsAsync(int maxSize = 200) => throw new NotImplementedException();
        public Task<int> RelinkMissingImagesAsync(string sourceDirectory) => throw new NotImplementedException();
        public Task<OrphanOriginalsResult> AnalyzeOrphanOriginalsAsync() => Task.FromResult(OrphanOriginals);
        public Task<OrphanOriginalsCleanupResult> CleanupOrphanOriginalsAsync() => throw new NotImplementedException();
        public Task CleanupLogsAsync() => Task.CompletedTask;
        public Task CleanupImageCacheAsync() => Task.CompletedTask;
        public Task<int> CountFilesAsync(string directory) => Task.FromResult(0);
        public Task<long> GetDirectorySizeAsync(string directory) => Task.FromResult(0L);
    }

    private sealed class FakeWeatherService : IWeatherService
    {
        public string? LastSearchedQuery { get; private set; }
        public IReadOnlyList<WeatherCitySuggestion> Suggestions { get; set; } = [];

        public Task<WeatherInfo?> GetCurrentWeatherAsync(string city) => Task.FromResult<WeatherInfo?>(null);

        public Task<IReadOnlyList<WeatherCitySuggestion>> SearchCitiesAsync(string query, int maxResults = 6)
        {
            LastSearchedQuery = query;
            return Task.FromResult(Suggestions);
        }

        public int GetFallbackTemperature(DateTimeOffset? date = null) => 22;
    }

    private sealed class FakeWeatherPreferencesService : IWeatherPreferencesService
    {
        public WeatherPreferences CurrentPreferences { get; set; } = new();

        public Task<WeatherPreferences> GetAsync() => Task.FromResult(CurrentPreferences);

        public Task SaveAsync(WeatherPreferences preferences)
        {
            CurrentPreferences = preferences;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeRecommendationPreferencesService : IRecommendationPreferencesService
    {
        public RecommendationPreferences SavedPreferences { get; private set; } = new();

        public Task<RecommendationPreferences> GetAsync() => Task.FromResult(SavedPreferences);

        public Task SaveAsync(RecommendationPreferences preferences)
        {
            SavedPreferences = preferences;
            return Task.CompletedTask;
        }
    }

    private static OutfitDisplayPreferencesService CreateDisplayPreferencesService(OutfitCardDisplayMode mode)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}-settings-outfit-display.json");
        var service = new OutfitDisplayPreferencesService(filePath);
        service.SaveAsync(new OutfitDisplayPreferences
        {
            DefaultCardDisplayMode = mode
        }).GetAwaiter().GetResult();
        return service;
    }

    private sealed class FakeBackupService : IBackupService
    {
        public BackupValidationResult Validation { get; set; } = CreateValidation();
        public IReadOnlyList<BackupHistoryItem> History { get; set; } = [];
        public IReadOnlyList<BackupHistoryItem>? HistoryAfterExport { get; set; }
        public IReadOnlyList<BackupHistoryItem>? HistoryAfterClear { get; set; }
        public BackupExportResult ExportResult { get; set; } = CreateExportResult(@"D:\backup\export.zip");
        public BackupImportResult ImportResult { get; set; } = CreateImportResult(@"D:\backup\import.zip");
        public string? ExportedPath { get; private set; }
        public string? ImportedPath { get; private set; }
        public bool ClearHistoryCalled { get; private set; }

        public Task<BackupValidationResult> ValidateExportAsync(string filePath) => Task.FromResult(Validation);
        public Task<BackupExportResult> ExportAsync(string filePath)
        {
            ExportedPath = filePath;
            if (HistoryAfterExport != null)
                History = HistoryAfterExport;
            return Task.FromResult(ExportResult);
        }

        public Task<BackupImportResult> ImportAsync(string filePath)
        {
            ImportedPath = filePath;
            return Task.FromResult(ImportResult);
        }

        public Task<IReadOnlyList<BackupHistoryItem>> GetHistoryAsync(int maxCount = 8) => Task.FromResult(History);
        public Task ClearHistoryAsync()
        {
            ClearHistoryCalled = true;
            History = HistoryAfterClear ?? [];
            return Task.CompletedTask;
        }

        public string BuildDefaultBackupPath() => @"D:\backup\default.zip";
    }
}
