using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure.Services;
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
        Assert.Equal("512 B", SettingsViewModel.FormatSize(512));
        Assert.Equal("2 KB", SettingsViewModel.FormatSize(2048));
        Assert.Equal("1.5 MB", SettingsViewModel.FormatSize(1572864));
    }

    private static SettingsViewModel CreateViewModel(FakeImageMaintenanceService imageMaintenance)
    {
        return new SettingsViewModel(
            new FakeBackupService(),
            imageMaintenance,
            new FakeWeatherService(),
            new FakeWeatherPreferencesService(),
            new ThemeService(new ThemePreferencesService(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.json"))));
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
    }

    private sealed class FakeWeatherService : IWeatherService
    {
        public Task<WeatherInfo?> GetCurrentWeatherAsync(string city) => Task.FromResult<WeatherInfo?>(null);
    }

    private sealed class FakeWeatherPreferencesService : IWeatherPreferencesService
    {
        public Task<WeatherPreferences> GetAsync() => Task.FromResult(new WeatherPreferences());
        public Task SaveAsync(WeatherPreferences preferences) => Task.CompletedTask;
    }

    private sealed class FakeBackupService : IBackupService
    {
        public Task<BackupValidationResult> ValidateExportAsync(string filePath) => throw new NotImplementedException();
        public Task<BackupExportResult> ExportAsync(string filePath) => throw new NotImplementedException();
        public Task<BackupImportResult> ImportAsync(string filePath) => throw new NotImplementedException();
        public Task<IReadOnlyList<BackupHistoryItem>> GetHistoryAsync(int maxCount = 8) => throw new NotImplementedException();
        public Task ClearHistoryAsync() => throw new NotImplementedException();
        public string BuildDefaultBackupPath() => throw new NotImplementedException();
    }
}
