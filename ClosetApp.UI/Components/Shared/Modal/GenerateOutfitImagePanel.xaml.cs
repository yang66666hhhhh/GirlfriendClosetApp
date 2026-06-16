using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
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
    private readonly IAiGenerationPreferencesService _preferencesService;
    private readonly Func<Task>? _onChanged;
    private readonly DispatcherTimer _busyTimer;
    private bool _canGenerate;
    private bool _isBusy;
    private string? _lastActionSummary;
    private DateTimeOffset? _busyStartedAt;
    private int _activeTimeoutSeconds = 60;

    public GenerateOutfitImagePanel(Guid outfitId, Func<Task>? onChanged = null)
    {
        _outfitId = outfitId;
        _readinessUseCase = App.Services.GetRequiredService<GetAiGenerationReadiness>();
        _getGeneratedImagesUseCase = App.Services.GetRequiredService<GetOutfitGeneratedImages>();
        _generateOutfitEffectImageUseCase = App.Services.GetRequiredService<GenerateOutfitEffectImage>();
        _saveUploadedOutfitGeneratedImageUseCase = App.Services.GetRequiredService<SaveUploadedOutfitGeneratedImage>();
        _setPrimaryOutfitGeneratedImageUseCase = App.Services.GetRequiredService<SetPrimaryOutfitGeneratedImage>();
        _deleteOutfitGeneratedImageUseCase = App.Services.GetRequiredService<DeleteOutfitGeneratedImage>();
        _preferencesService = App.Services.GetRequiredService<IAiGenerationPreferencesService>();
        _onChanged = onChanged;
        _busyTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _busyTimer.Tick += BusyTimer_Tick;
        InitializeComponent();
        Loaded += GenerateOutfitImagePanel_Loaded;
        Unloaded += GenerateOutfitImagePanel_Unloaded;
    }

    private async void GenerateOutfitImagePanel_Loaded(object sender, RoutedEventArgs e)
    {
        BindOptions();
        await RefreshAsync();
    }

    private void GenerateOutfitImagePanel_Unloaded(object sender, RoutedEventArgs e)
    {
        _busyTimer.Stop();
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
        var preferences = await _preferencesService.GetAsync();
        _activeTimeoutSeconds = preferences.TimeoutSeconds;
        var readiness = await _readinessUseCase.ExecuteAsync(_outfitId);
        _canGenerate = readiness.CanGenerate;
        TxtReadinessBody.Text = readiness.Summary;
        TxtActiveProvider.Text = BuildActiveProviderText(readiness.Preferences);
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
                image.IsPrimary ? "当前首选图" : "设为首选图",
                !image.IsPrimary,
                image.IsPrimary ? "首选效果图" : "历史效果图",
                "再次使用完全相同的档案、搭配和生成条件时，会直接复用这张结果。"))
            .ToList();

        SavedImagesList.ItemsSource = items;
        SavedImagesSection.Visibility = items.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        TxtSavedImagesSummary.Text = items.Count > 0
            ? $"这套搭配当前已保存 {items.Count} 张效果图，可以直接保留历史，不必每次重新生成。"
            : string.Empty;

        var recentFailures = images
            .Where(image => !string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(image => image.CreatedAt)
            .Take(3)
            .Select(BuildAttemptListItem)
            .ToList();

        RecentFailuresList.ItemsSource = recentFailures;
        RecentFailuresSection.Visibility = recentFailures.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        TxtRecentFailuresSummary.Text = recentFailures.Count > 0
            ? "这里会保留最近几次未成功的尝试，方便你直接重试，不用重新选参数。"
            : string.Empty;
    }

    private async void Generate_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyFeedback(
                "正在生成并保存",
                "我已经开始请求远端生成效果图了。这个过程可能需要几十秒到两分钟，先不要关闭弹窗。");
            ApplyBusyState(true);
            var request = BuildRequest();
            var image = await _generateOutfitEffectImageUseCase.ExecuteAsync(request);
            await RefreshAsync();
            await NotifyChangedAsync();

            if (image.WasReused)
            {
                ShowIdleStatus(
                    "已直接复用历史结果",
                    "这套搭配在相同条件下已经生成过，系统直接使用了之前保存的效果图。");
                ToastService.Instance.ShowSuccess("已复用已保存效果图", "这套搭配在相同条件下已经生成过，直接使用了历史结果。");
            }
            else
            {
                ShowIdleStatus(
                    "效果图已生成并保存",
                    "新的效果图已经写入当前搭配历史，你现在可以直接设为首选图或继续生成别的版本。");
                ToastService.Instance.ShowSuccess("效果图已生成", "新的搭配效果图已经保存到这套搭配下。");
            }
        }
        catch (Exception ex)
        {
            ShowIdleStatus("效果图生成失败", BuildFriendlyFailureMessage(ex));
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
            SetBusyFeedback("正在设置首选效果图", "正在保存这张图在效果图优先模式下的展示顺序。");
            ApplyBusyState(true);
            await _setPrimaryOutfitGeneratedImageUseCase.ExecuteAsync(imageId);
            await RefreshAsync();
            await NotifyChangedAsync();
            ShowIdleStatus("已设为首选效果图", "在效果图优先模式下，这张图现在会优先展示。");
            ToastService.Instance.ShowSuccess("已设为首选效果图", "在效果图优先模式下，这张图会优先展示在卡片、工作台和大图预览里。");
        }
        catch (Exception ex)
        {
            ShowIdleStatus("设置首选效果图失败", BuildFriendlyFailureMessage(ex));
            ToastService.Instance.ShowError("设置首选效果图失败", ex.Message);
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
            SetBusyFeedback("正在删除效果图", "正在移除这张历史效果图并同步刷新列表。");
            ApplyBusyState(true);
            await _deleteOutfitGeneratedImageUseCase.ExecuteAsync(imageId);
            await RefreshAsync();
            await NotifyChangedAsync();
            ShowIdleStatus("已删除效果图", "这张历史效果图已经从当前搭配里移除。");
            ToastService.Instance.ShowSuccess("已删除效果图", "这张历史效果图已经移除。");
        }
        catch (Exception ex)
        {
            ShowIdleStatus("删除效果图失败", BuildFriendlyFailureMessage(ex));
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
        CmbScene.IsEnabled = !isBusy;
        CmbPose.IsEnabled = !isBusy;
        CmbBackgroundStyle.IsEnabled = !isBusy;
        CmbFraming.IsEnabled = !isBusy;
        CmbMood.IsEnabled = !isBusy;
        BtnGenerate.Content = isBusy ? "正在处理..." : "生成并保存";
        BtnUpload.Content = isBusy ? "处理中..." : "上传并保存效果图";
        BusyProgressBar.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;

        if (isBusy)
        {
            _busyStartedAt = DateTimeOffset.Now;
            UpdateBusyMetaText();
            _busyTimer.Start();
        }
        else
        {
            _busyTimer.Stop();
            _busyStartedAt = null;
        }
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
            SetBusyFeedback("正在上传并保存", "图片已经选中，正在保存到当前搭配的效果图历史中。");
            ApplyBusyState(true);
            await _saveUploadedOutfitGeneratedImageUseCase.ExecuteAsync(
                new SaveUploadedOutfitGeneratedImageRequest(_outfitId, dialog.FileName));
            await RefreshAsync();
            await NotifyChangedAsync();
            ShowIdleStatus("效果图已上传并保存", "这张图片已经进入当前搭配的效果图历史。");
            ToastService.Instance.ShowSuccess("效果图已上传", "这张图片已经保存到当前搭配的效果图历史中。");
        }
        catch (Exception ex)
        {
            ShowIdleStatus("上传效果图失败", BuildFriendlyFailureMessage(ex));
            ToastService.Instance.ShowError("上传效果图失败", ex.Message);
        }
        finally
        {
            ApplyBusyState(false);
        }
    }

    private async void RetrySavedAttempt_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { Tag: Guid imageId })
            return;

        var images = await _getGeneratedImagesUseCase.ExecuteAsync(_outfitId);
        var targetAttempt = images.FirstOrDefault(image => image.Id == imageId);
        if (targetAttempt == null)
            return;

        ApplySavedAttemptOptions(targetAttempt);
        Generate_Click(sender, e);
    }

    private async void DeleteFailedAttempt_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { Tag: Guid imageId })
            return;

        var confirmed = await ConfirmModal.ShowAsync(
            "删除失败记录",
            "删除后不会影响已经保存成功的效果图。",
            "只会把这次失败尝试从最近生成状态列表里移除，确定继续吗？",
            confirmText: "删除记录");
        if (!confirmed)
            return;

        try
        {
            SetBusyFeedback("正在删除失败记录", "正在把这次失败尝试从历史列表里移除。");
            ApplyBusyState(true);
            await _deleteOutfitGeneratedImageUseCase.ExecuteAsync(imageId);
            await RefreshAsync();
            await NotifyChangedAsync();
            ShowIdleStatus("已删除失败记录", "这条失败尝试已经从最近生成状态里移除。");
            ToastService.Instance.ShowSuccess("已删除失败记录", "这次失败尝试已经从历史列表里移除。");
        }
        catch (Exception ex)
        {
            ShowIdleStatus("删除失败记录失败", BuildFriendlyFailureMessage(ex));
            ToastService.Instance.ShowError("删除失败记录失败", ex.Message);
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
        return OutfitGeneratedImageDisplayHelper.BuildBitmap(relativePath, 280, preferThumbnail: true);
    }

    private static string BuildActiveProviderText(AiGenerationPreferences preferences)
    {
        var protocol = string.Equals(preferences.Model, "gpt-image-2", StringComparison.OrdinalIgnoreCase)
            ? "images/generations"
            : preferences.Model.StartsWith("gpt-image-", StringComparison.OrdinalIgnoreCase)
                ? "images/edits"
            : "responses";
        return $"当前生效配置：{preferences.BaseUrl} · {preferences.Model} · {protocol} 路由";
    }

    private void SetBusyFeedback(string title, string body)
    {
        _lastActionSummary = body;
        ActionStatusSection.Visibility = Visibility.Visible;
        TxtActionStatusTitle.Text = title;
        TxtActionStatusBody.Text = body;
        TxtActionStatusMeta.Visibility = Visibility.Visible;
        UpdateBusyMetaText();
    }

    private void ShowIdleStatus(string title, string body)
    {
        _lastActionSummary = body;
        ActionStatusSection.Visibility = Visibility.Visible;
        TxtActionStatusTitle.Text = title;
        TxtActionStatusBody.Text = body;
        BusyProgressBar.Visibility = Visibility.Collapsed;
        TxtActionStatusMeta.Visibility = Visibility.Collapsed;
        TxtActionStatusMeta.Text = string.Empty;
    }

    private static string BuildFriendlyFailureMessage(Exception ex)
    {
        if (ex is TaskCanceledException)
        {
            return "请求已经发出，但远端在超时时间内没有返回结果。你现在这个中转接口返回过 524，说明更像是上游处理太慢或中转超时，不是按钮没有点到。";
        }

        if (ex is HttpRequestException httpRequestException)
        {
            var message = httpRequestException.Message;
            if (message.Contains("524", StringComparison.OrdinalIgnoreCase))
                return "中转接口返回了 524，这通常表示请求已经到达服务端，但上游生成时间过长，被网关提前断开了。建议先把超时调高，或换更稳定的图片模型/通道。";
        }

        return ex.Message;
    }

    private GeneratedAttemptListItem BuildAttemptListItem(OutfitGeneratedImageDto image)
    {
        var optionSummary = TryBuildOptionSummary(image.OptionSnapshotJson, out var optionValues)
            ? optionValues
            : "保留上次条件";

        var failureReason = string.Equals(image.Status, "Pending", StringComparison.OrdinalIgnoreCase)
            ? "这次请求还在处理中。你可以稍等一会儿再刷新，或者直接再试一次。"
            : string.IsNullOrWhiteSpace(image.FailureReason)
                ? "上一次没有成功完成，但没有返回更具体的失败原因。"
                : image.FailureReason!;

        var statusText = string.Equals(image.Status, "Pending", StringComparison.OrdinalIgnoreCase)
            ? "生成中"
            : "生成失败";

        return new GeneratedAttemptListItem(
            image.Id,
            statusText,
            $"{image.CreatedAt:yyyy-MM-dd HH:mm} · {image.Model}",
            failureReason,
            optionSummary,
            true);
    }

    private void ApplySavedAttemptOptions(OutfitGeneratedImageDto image)
    {
        if (!TryParseOptions(image.OptionSnapshotJson, out var savedOptions))
            return;

        ArgumentNullException.ThrowIfNull(savedOptions);
        SelectComboValue(CmbScene, savedOptions.Scene);
        SelectComboValue(CmbPose, savedOptions.Pose);
        SelectComboValue(CmbBackgroundStyle, savedOptions.BackgroundStyle);
        SelectComboValue(CmbFraming, savedOptions.Framing);
        SelectComboValue(CmbMood, savedOptions.Mood);

        ShowIdleStatus(
            "已恢复上次生成条件",
            "已经把上一次的场景、姿态、背景、构图和情绪填回来了，马上会按同一组条件重试。");
    }

    private static void SelectComboValue(ComboBox comboBox, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        foreach (var item in comboBox.Items)
        {
            if (string.Equals(item?.ToString(), value, StringComparison.Ordinal))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static bool TryBuildOptionSummary(string json, out string summary)
    {
        summary = string.Empty;
        if (!TryParseOptions(json, out var options))
            return false;

        ArgumentNullException.ThrowIfNull(options);
        summary = $"{options.Scene} · {options.Pose} · {options.BackgroundStyle}";
        return true;
    }

    private static bool TryParseOptions(string json, out GenerateOutfitEffectImageRequest? options)
    {
        options = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            options = JsonSerializer.Deserialize<GenerateOutfitEffectImageRequest>(json);
            return options != null;
        }
        catch
        {
            return false;
        }
    }

    private void BusyTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isBusy)
        {
            _busyTimer.Stop();
            return;
        }

        UpdateBusyMetaText();
    }

    private void UpdateBusyMetaText()
    {
        if (!_isBusy || _busyStartedAt == null)
        {
            TxtActionStatusMeta.Visibility = Visibility.Collapsed;
            TxtActionStatusMeta.Text = string.Empty;
            return;
        }

        var elapsed = DateTimeOffset.Now - _busyStartedAt.Value;
        var seconds = Math.Max(1, (int)Math.Floor(elapsed.TotalSeconds));
        var tone = elapsed.TotalSeconds >= Math.Max(30, _activeTimeoutSeconds * 0.75)
            ? "已经接近当前超时阈值，若中转继续无响应，可能会返回 524 或超时失败。"
            : "请求已经发出，当前弹窗会持续等待远端返回。";
        TxtActionStatusMeta.Visibility = Visibility.Visible;
        TxtActionStatusMeta.Text = $"已等待 {seconds} 秒 · 当前超时 {Math.Max(12, _activeTimeoutSeconds)} 秒 · {tone}";
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
        public string PrimaryBadgeText => "当前首选图";
    }

    private sealed record GeneratedAttemptListItem(
        Guid Id,
        string StatusText,
        string MetaText,
        string FailureReason,
        string OptionSummary,
        bool CanRetry);
}
