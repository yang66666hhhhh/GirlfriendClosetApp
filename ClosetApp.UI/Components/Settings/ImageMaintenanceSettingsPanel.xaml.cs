using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Settings;

public partial class ImageMaintenanceSettingsPanel : UserControl
{
    private readonly IImageMaintenanceService _imageMaintenanceService;
    private readonly IOutfitService _outfitService;
    private bool _isBusy;

    public ImageMaintenanceSettingsPanel()
    {
        InitializeComponent();
        _imageMaintenanceService = App.Services.GetRequiredService<IImageMaintenanceService>();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
    }

    public event EventHandler? WardrobeImagesChanged;

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    public Task RefreshAsync()
    {
        return ViewModel.RefreshStatsAsync();
    }

    private async void RefreshStats_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在刷新图片状态...");
            await RefreshAsync();
            ToastService.Instance.ShowInfo("图片状态已刷新。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新图片状态失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void ClearThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var confirmed = await ConfirmModal.ShowAsync(
            "清理缓存",
            "原始图片不会被删除。",
            "这会清理当前生成的显示图和缩略图缓存，后续浏览时会按需重新生成。确定继续吗？",
            confirmText: "清理缓存");

        if (!confirmed)
            return;

        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在清理图片缓存...");
            await _imageMaintenanceService.CleanupImageCacheAsync();
            ClothingImageLoader.ClearMemoryCaches();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("图片缓存已清理");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("清理图片缓存失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void RebuildThumbnails_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在重建缺失缓存...");
            var result = await _imageMaintenanceService.RebuildMissingThumbnailsAsync();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("图片缓存已重建", result.Summary);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("重建图片缓存失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void CleanupOrphanOriginals_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        OrphanOriginalsResult analysis;
        try
        {
            SetBusyState(true, "正在分析孤儿原图...");
            analysis = await _imageMaintenanceService.AnalyzeOrphanOriginalsAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("分析孤儿原图失败", ex.Message);
            return;
        }
        finally
        {
            SetBusyState(false);
        }

        if (!analysis.HasOrphans)
        {
            ToastService.Instance.ShowInfo("没有发现可清理的孤儿原图。");
            return;
        }

        var confirmed = await ConfirmModal.ShowAsync(
            "清理孤儿原图",
            $"发现 {analysis.OrphanCount} 张数据库未引用的原图，占用 {FileSizeFormatter.Format(analysis.TotalBytes)}。",
            "清理会同时删除这些原图对应的主视觉和小预览缓存，但不会删除任何仍被衣物或穿着历史引用的图片。确定继续吗？",
            confirmText: "清理原图");

        if (!confirmed)
            return;

        try
        {
            SetBusyState(true, "正在清理孤儿原图...");
            var result = await _imageMaintenanceService.CleanupOrphanOriginalsAsync();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("孤儿原图已清理", result.Summary);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("清理孤儿原图失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void RepairMissingImages_Click(object sender, RoutedEventArgs e)
    {
        await RepairMissingImagesAsync();
    }

    public async Task RepairMissingImagesAsync()
    {
        if (_isBusy)
            return;

        var dialog = new OpenFolderDialog
        {
            Title = "选择旧图片所在目录，应用会按文件名尝试重连缺失图片。"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
            return;

        try
        {
            SetBusyState(true, "正在修复缺失图片...");
            var repairedCount = await _imageMaintenanceService.RelinkMissingImagesAsync(dialog.FolderName);
            await RefreshAsync();
            WardrobeImagesChanged?.Invoke(this, EventArgs.Empty);

            ToastService.Instance.ShowSuccess(
                repairedCount == 0 ? "没有需要修复的图片" : "缺失图片已重连",
                repairedCount == 0 ? null : $"共修复 {repairedCount} 张图片。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("修复缺失图片失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void CheckWornRecordImages_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在检查历史图片...");
            var result = await _outfitService.AnalyzeWornRecordImageHealthAsync();
            SetBusyState(false);

            if (!result.HasMissingImages)
            {
                ToastService.Instance.ShowSuccess("历史图片检查完成", result.Summary);
                return;
            }

            var previewItems = result.MissingRecordItems
                .Take(8)
                .Select(item => item.Summary)
                .ToList();
            var moreText = result.MissingRecordItems.Count > previewItems.Count
                ? $"\n...还有 {result.MissingRecordItems.Count - previewItems.Count} 条记录"
                : string.Empty;
            ToastService.Instance.ShowInfo("发现历史缺图", result.Summary);
            var confirmed = await ConfirmModal.ShowAsync(
                "穿着历史图片",
                result.Summary,
                $"{string.Join("\n", previewItems)}{moreText}\n\n是否打开最近一条缺图记录所在日期？",
                confirmText: "打开记录");
            if (confirmed)
                await OpenWornRecordDayAsync(result.MissingRecordItems[0].WornDate);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("历史图片检查失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async Task OpenWornRecordDayAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        var records = (await _outfitService.GetWornRecordsAsync(start, end)).ToList();
        ModalService.Instance.Show(new WornDayDetailsDialog(start, records));
    }

    private void SetBusyState(bool isBusy, string? statusText = null)
    {
        _isBusy = isBusy;

        BtnRefreshStats.IsEnabled = !isBusy;
        BtnRebuildThumbnails.IsEnabled = !isBusy;
        BtnRepairMissingImages.IsEnabled = !isBusy;
        BtnCheckWornRecordImages.IsEnabled = !isBusy;
        BtnClearThumbnails.IsEnabled = !isBusy;
        BtnCleanupOrphanOriginals.IsEnabled = !isBusy;

        TxtImageOperationStatus.Text = statusText ?? string.Empty;
        TxtImageOperationStatus.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    }
}
