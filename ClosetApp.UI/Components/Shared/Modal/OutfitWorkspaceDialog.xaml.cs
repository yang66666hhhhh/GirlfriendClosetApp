using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Shared.Modal;

using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;
using OutfitGeneratedImageEntity = global::ClosetApp.Domain.Entities.OutfitGeneratedImage;

public partial class OutfitWorkspaceDialog : UserControl
{
    private readonly OutfitsViewModel _viewModel;
    private Guid? _selectedImageId;
    private IReadOnlyList<OutfitGeneratedImageEntity> _succeededImages = Array.Empty<OutfitGeneratedImageEntity>();

    public OutfitWorkspaceDialog(OutfitEntity outfit)
    {
        _viewModel = App.Services.GetRequiredService<OutfitsViewModel>();
        Outfit = outfit;
        InitializeComponent();
        Loaded += OutfitWorkspaceDialog_Loaded;
    }

    public OutfitEntity Outfit { get; private set; }

    private void OutfitWorkspaceDialog_Loaded(object sender, RoutedEventArgs e)
    {
        BindOutfit(Outfit);
    }

    private void BindOutfit(OutfitEntity outfit)
    {
        var state = OutfitGeneratedImageDisplayHelper.BuildState(outfit.GeneratedImages);
        _succeededImages = OutfitGeneratedImageDisplayHelper.GetSucceededImages(outfit.GeneratedImages);

        TxtTitle.Text = BuildDisplayName(outfit);
        TxtSceneChip.Text = GetSceneLabel(outfit.Scene);
        TxtSeasonChip.Text = GetSeasonLabel(outfit.Season);
        TxtAiState.Text = state.Label;
        TxtSummary.Text = outfit.WearCount > 0
            ? $"已经穿过 {outfit.WearCount} 次，最近一次是 {FormatWornDate(outfit.WornDate)}。当前浮窗专注效果图与 AI 管理。"
            : "还没有穿着记录。这里专注效果图与 AI 管理，不再展示原始搭配预览。";

        ApplyFavoriteVisual(outfit);
        ApplyBadgeVisual(AiStateBadge, TxtAiState, state.VisualStateKey);

        var primaryImage = OutfitGeneratedImageDisplayHelper.GetPrimaryOrFirst(_succeededImages);
        _selectedImageId = primaryImage?.Id ?? _succeededImages.FirstOrDefault()?.Id;
        TxtCurrentImageMeta.Text = primaryImage != null
            ? BuildHeroMetaText(primaryImage)
            : string.Empty;
        TxtCurrentImageHint.Text = primaryImage != null
            ? $"已保存 {_succeededImages.Count} 张效果图。点击主图查看大图，点右侧历史图切换当前展示。"
            : string.Empty;
        TxtAiPanelSummary.Text = _succeededImages.Count > 0
            ? $"已保存 {_succeededImages.Count} 张效果图，当前打开就是这套搭配的成图工作台。"
            : "还没有效果图，可以直接生成或上传。";
        TxtHistorySummary.Text = _succeededImages.Count > 1
            ? $"共 {_succeededImages.Count} 张效果图，点缩略图切换当前展示。"
            : "当前只有这一张效果图。";

        var historyItems = _succeededImages
            .Select(image => new GeneratedImageHistoryItem(
                image.Id,
                image.IsPrimary ? "主效果图" : "历史效果图",
                $"{image.Model} · {image.CreatedAt:MM-dd HH:mm}",
                OutfitGeneratedImageDisplayHelper.BuildBitmap(image.ResultImagePath!, 220, preferThumbnail: true),
                image.Id == _selectedImageId))
            .ToList();

        HistoryList.ItemsSource = historyItems;
        var hasImages = historyItems.Count > 0;
        EmptyAiStateCard.Visibility = hasImages ? Visibility.Collapsed : Visibility.Visible;
        ImageWorkspaceHost.Visibility = hasImages ? Visibility.Visible : Visibility.Collapsed;
        HistorySectionHost.Visibility = hasImages ? Visibility.Visible : Visibility.Collapsed;
        HistoryScrollViewer.Visibility = historyItems.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        ApplySelectedImage(primaryImage);
    }

    private void HistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid imageId })
            return;

        _selectedImageId = imageId;
        var selectedImage = _succeededImages.FirstOrDefault(image => image.Id == imageId);
        ApplySelectedImage(selectedImage);
        RebindHistorySelection();
    }

    private async void Favorite_Click(object sender, RoutedEventArgs e)
    {
        var result = await _viewModel.ToggleFavoriteWithFeedbackAsync(Outfit);
        if (result == null)
            return;

        var refreshed = await _viewModel.RefreshSingleOutfitAsync(Outfit.Id);
        if (refreshed != null)
        {
            Outfit = refreshed;
            BindOutfit(Outfit);
        }
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(Outfit), async result =>
        {
            if (result.Type != EditorResultType.Saved || result.Entity == null)
                return;

            await _viewModel.RefreshAfterOutfitSavedWithFeedbackAsync(
                result.Entity.Id,
                $"已更新「{result.Entity.Name}」",
                "修改后的搭配已经同步到列表。");

            var refreshed = await _viewModel.RefreshSingleOutfitAsync(result.Entity.Id);
            if (refreshed != null)
            {
                Outfit = refreshed;
                BindOutfit(Outfit);
            }
        });
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (!await ConfirmModal.ShowDeleteAsync($"确定删除搭配「{Outfit.Name}」吗？"))
            return;

        await _viewModel.DeleteOutfitWithFeedbackAsync(Outfit);
        ModalService.Instance.Hide();
    }

    private async void WearToday_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.RecordOutfitWornWithFeedbackAsync(Outfit, Outfit.Name);
        var refreshed = await _viewModel.RefreshSingleOutfitAsync(Outfit.Id);
        if (refreshed != null)
        {
            Outfit = refreshed;
            BindOutfit(Outfit);
        }
    }

    private void OpenPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_succeededImages.Count == 0)
            return;

        var selectedId = _selectedImageId ?? _succeededImages.First().Id;
        ModalService.Instance.Show(new GeneratedImagePreviewDialog(_succeededImages, selectedId));
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        OpenAiManagementPanel();
    }

    private void Upload_Click(object sender, RoutedEventArgs e)
    {
        OpenAiManagementPanel();
    }

    private void ManageAi_Click(object sender, RoutedEventArgs e)
    {
        OpenAiManagementPanel();
    }

    private void OpenAiManagementPanel()
    {
        ModalService.Instance.Show(new GenerateOutfitImagePanel(Outfit.Id, RefreshCurrentOutfitAsync));
    }

    private async Task RefreshCurrentOutfitAsync()
    {
        var refreshed = await _viewModel.RefreshSingleOutfitAsync(Outfit.Id);
        if (refreshed != null)
        {
            Outfit = refreshed;
            BindOutfit(Outfit);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private void ApplyFavoriteVisual(OutfitEntity outfit)
    {
        var isFav = outfit.Favorites.Count > 0;
        BtnFavorite.Content = isFav ? "♥" : "♡";
        BtnFavorite.Foreground = isFav
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("TextPlaceholderBrush");
        BtnFavorite.Background = isFav
            ? new SolidColorBrush(Color.FromRgb(255, 243, 246))
            : new SolidColorBrush(Color.FromRgb(247, 251, 255));
        BtnFavorite.BorderBrush = isFav
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("BorderLightBrush");
    }

    private static void ApplyBadgeVisual(Border badge, TextBlock text, string stateKey)
    {
        switch (stateKey)
        {
            case "AiState.Success":
                badge.Background = (Brush)System.Windows.Application.Current.FindResource("PrimaryLightBrush");
                badge.BorderBrush = (Brush)System.Windows.Application.Current.FindResource("PrimaryBrush");
                text.Foreground = (Brush)System.Windows.Application.Current.FindResource("PrimaryBrush");
                break;
            case "AiState.Pending":
                badge.Background = (Brush)System.Windows.Application.Current.FindResource("TagAmberSurfaceBrush");
                badge.BorderBrush = (Brush)System.Windows.Application.Current.FindResource("TagAmberBorderBrush");
                text.Foreground = (Brush)System.Windows.Application.Current.FindResource("TagAmberTextBrush");
                break;
            case "AiState.Failed":
                badge.Background = (Brush)System.Windows.Application.Current.FindResource("TagRoseSurfaceBrush");
                badge.BorderBrush = (Brush)System.Windows.Application.Current.FindResource("TagRoseBorderBrush");
                text.Foreground = (Brush)System.Windows.Application.Current.FindResource("TagRoseTextBrush");
                break;
            default:
                badge.Background = (Brush)System.Windows.Application.Current.FindResource("SurfaceSectionBrush");
                badge.BorderBrush = (Brush)System.Windows.Application.Current.FindResource("BorderLightBrush");
                text.Foreground = (Brush)System.Windows.Application.Current.FindResource("TextSecondaryBrush");
                break;
        }
    }

    private static string BuildDisplayName(OutfitEntity outfit)
    {
        if (!string.IsNullOrWhiteSpace(outfit.Name))
            return outfit.Name.Trim();

        var names = outfit.OutfitClothes
            .Select(link => link.Clothing?.Name?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Take(2)
            .ToArray();

        return names.Length > 0 ? string.Join(" · ", names) : "未命名搭配";
    }

    private static string FormatWornDate(DateTime? wornDate)
    {
        return wornDate?.ToString("yyyy-MM-dd") ?? "未穿过";
    }

    private static string GetSceneLabel(OutfitScene scene) => scene switch
    {
        OutfitScene.Work => "通勤",
        OutfitScene.Date => "约会",
        OutfitScene.Travel => "出游",
        OutfitScene.Party => "派对",
        OutfitScene.Casual => "休闲",
        _ => scene.ToString()
    };

    private static string GetSeasonLabel(Season season) => season switch
    {
        Season.Spring => "春季",
        Season.Summer => "夏季",
        Season.Autumn => "秋季",
        Season.Winter => "冬季",
        Season.AllSeason => "四季",
        _ => "未设季节"
    };

    private void ApplySelectedImage(OutfitGeneratedImageEntity? image)
    {
        if (image?.ResultImagePath is { } heroPath)
        {
            HeroImage.Source = OutfitGeneratedImageDisplayHelper.BuildBitmap(heroPath, 900, preferThumbnail: false);
            HeroPrimaryBadge.Visibility = image.IsPrimary ? Visibility.Visible : Visibility.Collapsed;
            TxtCurrentImageMeta.Text = BuildHeroMetaText(image);
            TxtCurrentImageHint.Text = image.IsPrimary
                ? "这是当前主效果图。点击主图查看大图，右侧可以切换其他历史效果图。"
                : "这是历史效果图。点击主图查看大图，右侧可以继续切换。";
            return;
        }

        HeroImage.Source = null;
        HeroPrimaryBadge.Visibility = Visibility.Collapsed;
        TxtCurrentImageMeta.Text = string.Empty;
        TxtCurrentImageHint.Text = string.Empty;
    }

    private void RebindHistorySelection()
    {
        var historyItems = _succeededImages
            .Select(image => new GeneratedImageHistoryItem(
                image.Id,
                image.IsPrimary ? "主效果图" : "历史效果图",
                $"{image.Model} · {image.CreatedAt:MM-dd HH:mm}",
                OutfitGeneratedImageDisplayHelper.BuildBitmap(image.ResultImagePath!, 220, preferThumbnail: true),
                image.Id == _selectedImageId))
            .ToList();

        HistoryList.ItemsSource = historyItems;
    }

    private static string BuildHeroMetaText(OutfitGeneratedImageEntity image)
    {
        var label = image.IsPrimary ? "主效果图" : "历史效果图";
        return $"{label} · {image.Model} · {image.CreatedAt:MM-dd HH:mm}";
    }

    private sealed record GeneratedImageHistoryItem(
        Guid Id,
        string BadgeText,
        string MetaText,
        System.Windows.Media.Imaging.BitmapImage? ThumbnailSource,
        bool IsSelected);
}
