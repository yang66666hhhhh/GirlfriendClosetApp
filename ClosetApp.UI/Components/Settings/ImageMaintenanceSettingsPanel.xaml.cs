using System.Windows;
using System.Windows.Controls;
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
        await RefreshAsync();
        ToastService.Instance.ShowInfo("图片状态已刷新。");
    }

    private async void ClearThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定清理图片缓存吗？原始图片不会被删除。",
            "清理缓存",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
            return;

        await _imageMaintenanceService.CleanupImageCacheAsync();
        ClothingImageLoader.ClearMemoryCaches();
        await RefreshAsync();
        MessageBox.Show("图片缓存已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("图片缓存已清理");
    }

    private async void RebuildThumbnails_Click(object sender, RoutedEventArgs e)
    {
        var result = await _imageMaintenanceService.RebuildMissingThumbnailsAsync();
        await RefreshAsync();
        MessageBox.Show(result.Summary, "图片缓存", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("图片缓存已重建", result.Summary);
    }

    private async void CleanupOrphanOriginals_Click(object sender, RoutedEventArgs e)
    {
        var analysis = await _imageMaintenanceService.AnalyzeOrphanOriginalsAsync();
        if (!analysis.HasOrphans)
        {
            MessageBox.Show("没有发现可清理的孤儿原图。", "原图治理", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"发现 {analysis.OrphanCount} 张数据库未引用的原图，占用 {FileSizeFormatter.Format(analysis.TotalBytes)}。\n\n清理会同时删除这些原图对应的主视觉和小预览缓存，但不会删除任何仍被衣物或穿着历史引用的图片。确定继续吗？",
            "清理孤儿原图",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        var result = await _imageMaintenanceService.CleanupOrphanOriginalsAsync();
        await RefreshAsync();
        MessageBox.Show(result.Summary, "原图治理", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("孤儿原图已清理", result.Summary);
    }

    private async void RepairMissingImages_Click(object sender, RoutedEventArgs e)
    {
        await RepairMissingImagesAsync();
    }

    public async Task RepairMissingImagesAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择旧图片所在目录，应用会按文件名尝试重连缺失图片。"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrWhiteSpace(dialog.FolderName))
            return;

        var repairedCount = await _imageMaintenanceService.RelinkMissingImagesAsync(dialog.FolderName);
        await RefreshAsync();
        WardrobeImagesChanged?.Invoke(this, EventArgs.Empty);

        MessageBox.Show(
            repairedCount == 0
                ? "没有找到可重连的图片文件。"
                : $"已修复 {repairedCount} 张缺失图片。",
            "图片修复",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess(
            repairedCount == 0 ? "没有需要修复的图片" : "缺失图片已重连",
            repairedCount == 0 ? null : $"共修复 {repairedCount} 张图片。");
    }

    private async void CheckWornRecordImages_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = await _outfitService.AnalyzeWornRecordImageHealthAsync();
            if (!result.HasMissingImages)
            {
                MessageBox.Show(result.Summary, "穿着历史图片", MessageBoxButton.OK, MessageBoxImage.Information);
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
            var confirm = MessageBox.Show(
                $"{result.Summary}\n\n{string.Join("\n", previewItems)}{moreText}\n\n是否打开最近一条缺图记录所在日期？",
                "穿着历史图片",
                MessageBoxButton.OKCancel,
                MessageBoxImage.Warning);

            ToastService.Instance.ShowInfo("发现历史缺图", result.Summary);
            if (confirm == MessageBoxResult.OK)
                await OpenWornRecordDayAsync(result.MissingRecordItems[0].WornDate);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("历史图片检查失败", ex.Message);
        }
    }

    private async Task OpenWornRecordDayAsync(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        var records = (await _outfitService.GetWornRecordsAsync(start, end)).ToList();
        ModalService.Instance.Show(new WornDayDetailsDialog(start, records));
    }
}
