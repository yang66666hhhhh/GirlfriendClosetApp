using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.UseCases.Insights;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel
{
    private readonly GetWardrobeInsights _getWardrobeInsights;
    private readonly GetAnnualOutfitReport _getAnnualOutfitReport;
    private WardrobeInsightsDto? _cachedInsights;

    public async Task DeleteOutfitAsync(Outfit outfit)
    {
        await _outfitService.DeleteOutfitAsync(outfit.Id);
        await LoadOutfitsAsync();
    }

    public async Task DeleteOutfitWithFeedbackAsync(Outfit outfit)
    {
        try
        {
            await DeleteOutfitAsync(outfit);
            ToastService.Instance.ShowSuccess($"已删除「{outfit.Name}」", "这套搭配已经从列表移除。");
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitDelete(ex, outfit.Name);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    public async Task RecordWornDateAsync(Outfit outfit, DateTime date)
    {
        await _outfitService.RecordWornDateAsync(outfit.Id, date);
        await LoadOutfitsAsync();
    }

    public Task RecordOutfitWornTodayAsync(Outfit outfit) => RecordWornDateAsync(outfit, DateTime.Now);

    public async Task RecordOutfitWornWithFeedbackAsync(
        Outfit outfit,
        string displayName,
        string detail = "今天的穿着记录已经更新。")
    {
        try
        {
            await RecordOutfitWornTodayAsync(outfit);
            ToastService.Instance.ShowSuccess($"已记录穿过「{displayName}」", detail);
        }
        catch (Exception ex)
        {
            var feedback = WardrobeActionErrorPresenter.ForOutfitRecord(ex, displayName);
            ToastService.Instance.ShowError(feedback.Title, feedback.Detail);
        }
    }

    [RelayCommand]
    public Task RecordRecommendedOutfitWornAsync(RecommendedOutfitDto? recommendation)
    {
        return recommendation == null
            ? Task.CompletedTask
            : RecordOutfitWornWithFeedbackAsync(
                recommendation.Outfit,
                recommendation.Name,
                "今日推荐已经同步到穿着记录。");
    }

    [RelayCommand]
    public async Task ShowWardrobeInsightsAsync()
    {
        try
        {
            _cachedInsights ??= await _getWardrobeInsights.ExecuteAsync();
            ModalService.Instance.Show(new ClosetApp.UI.Components.Shared.Modal.WardrobeInsightsDialog(_cachedInsights));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("衣柜统计数据加载失败", $"无法生成当前衣柜的统计分析：{ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ShowAnnualReportAsync()
    {
        try
        {
            var year = DateTime.Now.Year;
            var report = await _getAnnualOutfitReport.ExecuteAsync(year);
            ModalService.Instance.Show(new ClosetApp.UI.Components.Shared.Modal.AnnualOutfitReportDialog(report));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError($"{DateTime.Now.Year}年度报告加载失败", $"无法生成年度穿搭报告：{ex.Message}");
        }
    }

    public Task RefreshAfterOutfitSavedAsync() => LoadOutfitsAsync();

    public async Task RefreshAfterOutfitSavedWithFeedbackAsync(string title, string detail)
    {
        try
        {
            await RefreshAfterOutfitSavedAsync();
            ToastService.Instance.ShowSuccess(title, detail);
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("搭配列表刷新失败", $"保存成功但列表未能更新：{ex.Message}");
        }
    }

    public async Task<bool> ToggleFavoriteAsync(Outfit outfit)
    {
        var isFav = await _outfitService.ToggleFavoriteAsync(outfit.Id);
        await LoadOutfitsAsync();
        return isFav;
    }

    public async Task<bool?> ToggleFavoriteWithFeedbackAsync(Outfit outfit)
    {
        try
        {
            var isFav = await ToggleFavoriteAsync(outfit);
            var name = outfit.Name?.Trim();
            var displayName = !string.IsNullOrWhiteSpace(name) ? $"「{name}」" : "该搭配";
            ToastService.Instance.ShowSuccess(isFav ? $"已收藏{displayName}" : $"已取消收藏{displayName}");
            return isFav;
        }
        catch (Exception ex)
        {
            var name = outfit.Name?.Trim();
            var displayName = !string.IsNullOrWhiteSpace(name) ? $"「{name}」" : "该搭配";
            ToastService.Instance.ShowError($"收藏{displayName}失败", ex.Message);
            return null;
        }
    }

    private void InvalidateInsightsCache()
    {
        _cachedInsights = null;
        InvalidateRecommendationDebugCache();
    }
}
