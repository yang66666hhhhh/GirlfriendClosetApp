using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class GenerateOutfitImagePanel : UserControl
{
    private readonly Guid _outfitId;
    private readonly GetAiGenerationReadiness _readinessUseCase;
    private readonly GetOutfitGeneratedImages _getGeneratedImagesUseCase;
    private readonly GenerateOutfitEffectImage _generateOutfitEffectImageUseCase;
    private readonly SaveUploadedOutfitGeneratedImage _saveUploadedOutfitGeneratedImageUseCase;
    private readonly SetPrimaryOutfitGeneratedImage _setPrimaryOutfitGeneratedImageUseCase;
    private readonly DeleteOutfitGeneratedImage _deleteOutfitGeneratedImageUseCase;
    private readonly Func<Task>? _onChanged;
    private bool _canGenerate;
    private bool _isBusy;

    public GenerateOutfitImagePanel(Guid outfitId, Func<Task>? onChanged = null)
    {
        _outfitId = outfitId;
        _readinessUseCase = App.Services.GetRequiredService<GetAiGenerationReadiness>();
        _getGeneratedImagesUseCase = App.Services.GetRequiredService<GetOutfitGeneratedImages>();
        _generateOutfitEffectImageUseCase = App.Services.GetRequiredService<GenerateOutfitEffectImage>();
        _saveUploadedOutfitGeneratedImageUseCase = App.Services.GetRequiredService<SaveUploadedOutfitGeneratedImage>();
        _setPrimaryOutfitGeneratedImageUseCase = App.Services.GetRequiredService<SetPrimaryOutfitGeneratedImage>();
        _deleteOutfitGeneratedImageUseCase = App.Services.GetRequiredService<DeleteOutfitGeneratedImage>();
        _onChanged = onChanged;
        InitializeComponent();
        Loaded += GenerateOutfitImagePanel_Loaded;
    }

    private async void GenerateOutfitImagePanel_Loaded(object sender, RoutedEventArgs e)
    {
        BindOptions();
        await RefreshAsync();
    }

    private void BindOptions()
    {
        CmbScene.ItemsSource = new[] { "通勤", "约会", "出游", "派对", "休闲", "街拍" };
        CmbPose.ItemsSource = new[] { "站姿正面", "轻微侧身", "自然行走", "扶肩回头", "坐姿半身" };
        CmbBackgroundStyle.ItemsSource = new[] { "城市街景", "简洁室内", "咖啡馆", "商场橱窗", "柔和纯色背景" };
        CmbFraming.ItemsSource = new[] { "全身", "三分之二身", "街拍竖构图", "杂志人像" };
        CmbMood.ItemsSource = new[] { "松弛", "利落", "温柔", "精致", "高级感" };

        CmbScene.SelectedIndex = 0;
        CmbPose.SelectedIndex = 0;
        CmbBackgroundStyle.SelectedIndex = 0;
        CmbFraming.SelectedIndex = 0;
        CmbMood.SelectedIndex = 0;
    }

    private async Task RefreshAsync()
    {
        var readiness = await _readinessUseCase.ExecuteAsync(_outfitId);
        _canGenerate = readiness.CanGenerate;
        TxtReadinessBody.Text = readiness.Summary;
        await LoadSavedImagesAsync();
        ApplyBusyState(_isBusy);
    }

    private async Task LoadSavedImagesAsync()
    {
        var images = await _getGeneratedImagesUseCase.ExecuteAsync(_outfitId);
        var items = images
            .Where(image => string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(image.ResultImagePath))
            .Select(image => new GeneratedImageListItem(
                image.Id,
                BuildPreviewImage(image.ResultImagePath!),
                image.IsPrimary,
                image.Model,
                $"{image.CreatedAt:yyyy-MM-dd HH:mm} · 已保存到这套搭配",
                image.IsPrimary ? "当前主图" : "设为主图",
                !image.IsPrimary,
                image.IsPrimary ? "主效果图" : "历史效果图",
                "再次使用完全相同的档案、搭配和生成条件时，会直接复用这张结果。"))
            .ToList();

        SavedImagesList.ItemsSource = items;
        SavedImagesSection.Visibility = items.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        TxtSavedImagesSummary.Text = items.Count > 0
            ? $"这套搭配当前已保存 {items.Count} 张效果图，可以直接保留历史，不必每次重新生成。"
            : string.Empty;
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            ApplyBusyState(true);
            var request = BuildRequest();
            var image = await _generateOutfitEffectImageUseCase.ExecuteAsync(request);
            await RefreshAsync();
            await NotifyChangedAsync();

            if (image.WasReused)
            {
                ToastService.Instance.ShowSuccess("已复用已保存效果图", "这套搭配在相同条件下已经生成过，直接使用了历史结果。");
            }
            else
            {
                ToastService.Instance.ShowSuccess("效果图已生成", "新的搭配效果图已经保存到这套搭配下。");
            }
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("效果图生成失败", ex.Message);
        }
        finally
        {
            ApplyBusyState(false);
        }
    }

    private async void SetPrimary_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { Tag: Guid imageId })
            return;

        try
        {
            ApplyBusyState(true);
            await _setPrimaryOutfitGeneratedImageUseCase.ExecuteAsync(imageId);
            await RefreshAsync();
            await NotifyChangedAsync();
            ToastService.Instance.ShowSuccess("已设为主效果图", "这张效果图会优先展示在效果图工作台和大图预览里。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("设置主效果图失败", ex.Message);
        }
        finally
        {
            ApplyBusyState(false);
        }
    }

    private async void DeleteSavedImage_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { Tag: Guid imageId })
            return;

        try
        {
            ApplyBusyState(true);
            await _deleteOutfitGeneratedImageUseCase.ExecuteAsync(imageId);
            await RefreshAsync();
            await NotifyChangedAsync();
            ToastService.Instance.ShowSuccess("已删除效果图", "这张历史效果图已经移除。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("删除效果图失败", ex.Message);
        }
        finally
        {
            ApplyBusyState(false);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private GenerateOutfitEffectImageRequest BuildRequest()
    {
        return new GenerateOutfitEffectImageRequest(
            _outfitId,
            CmbScene.SelectedItem?.ToString() ?? "通勤",
            CmbPose.SelectedItem?.ToString() ?? "站姿正面",
            CmbBackgroundStyle.SelectedItem?.ToString() ?? "城市街景",
            CmbFraming.SelectedItem?.ToString() ?? "全身",
            CmbMood.SelectedItem?.ToString() ?? "松弛");
    }

    private void ApplyBusyState(bool isBusy)
    {
        _isBusy = isBusy;
        BtnGenerate.IsEnabled = _canGenerate && !isBusy;
        BtnUpload.IsEnabled = !isBusy;
        SavedImagesList.IsEnabled = !isBusy;
    }

    private async void Upload_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        var dialog = new OpenFileDialog
        {
            Title = "选择效果图",
            Filter = "图片文件|*.jpg;*.jpeg;*.png;*.webp"
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            ApplyBusyState(true);
            await _saveUploadedOutfitGeneratedImageUseCase.ExecuteAsync(
                new SaveUploadedOutfitGeneratedImageRequest(_outfitId, dialog.FileName));
            await RefreshAsync();
            await NotifyChangedAsync();
            ToastService.Instance.ShowSuccess("效果图已上传", "这张图片已经保存到当前搭配的效果图历史中。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("上传效果图失败", ex.Message);
        }
        finally
        {
            ApplyBusyState(false);
        }
    }

    private async Task NotifyChangedAsync()
    {
        if (_onChanged != null)
            await _onChanged();
    }

    private static BitmapImage? BuildPreviewImage(string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        var thumbnailPath = Path.Combine(ClosetApp.Infrastructure.AppPaths.AiRendersThumbnailsDir, $"{fileName}_thumb{extension}");
        var displayPath = Path.Combine(ClosetApp.Infrastructure.AppPaths.AiRendersDisplayDir, relativePath);
        var candidatePath = File.Exists(thumbnailPath)
            ? thumbnailPath
            : File.Exists(displayPath)
                ? displayPath
                : null;
        if (candidatePath == null)
            return null;

        return AiImageBitmapCache.GetOrLoad(candidatePath, 280);
    }

    private sealed record GeneratedImageListItem(
        Guid Id,
        BitmapImage? ThumbnailSource,
        bool IsPrimary,
        string ModelText,
        string MetaText,
        string PrimaryActionText,
        bool CanSetPrimary,
        string StatusText,
        string ReuseHint)
    {
        public string PrimaryBadgeText => "当前主图";
    }
}
