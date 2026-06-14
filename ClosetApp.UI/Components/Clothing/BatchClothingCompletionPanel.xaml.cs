using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Clothing;

public partial class BatchClothingCompletionPanel : UserControl, IEditorPanel<BatchClothingCompletionRequest>
{
    private readonly IReadOnlyList<Guid> _clothingIds;
    private readonly string _queueLabel;
    private readonly ITagService _tagService;
    private ClothingType? _selectedType;
    private Season? _selectedSeason;
    private bool _isSubmitting;

    public event EventHandler<EditorResult<BatchClothingCompletionRequest>>? EditorCompleted;

    public BatchClothingCompletionPanel(IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> clothes, string queueLabel)
    {
        InitializeComponent();
        _clothingIds = clothes.Select(clothing => clothing.Id).ToList();
        _queueLabel = queueLabel;
        _tagService = App.Services.GetRequiredService<ITagService>();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        TxtSubtitle.Text = $"当前队列：{_queueLabel} · 共 {_clothingIds.Count} 件";
        TxtSummary.Text = $"会对「{_queueLabel}」里的 {_clothingIds.Count} 件衣服补全缺失资料。";
        TagSelection.LoadTags(await _tagService.GetStyleTagsAsync());
    }

    private void ApplyOption_Changed(object sender, RoutedEventArgs e)
    {
        CategoryPanel.IsEnabled = ChkType.IsChecked == true;
        CategoryPanel.Opacity = ChkType.IsChecked == true ? 1 : 0.45;
        SeasonPanel.IsEnabled = ChkSeason.IsChecked == true;
        SeasonPanel.Opacity = ChkSeason.IsChecked == true ? 1 : 0.45;
        TxtBrand.IsEnabled = ChkBrand.IsChecked == true;
        TxtBrand.Opacity = ChkBrand.IsChecked == true ? 1 : 0.45;
        TxtColor.IsEnabled = ChkColor.IsChecked == true;
        TxtColor.Opacity = ChkColor.IsChecked == true ? 1 : 0.45;
        TagSectionHost.IsEnabled = ChkTags.IsChecked == true;
        TagSectionHost.Opacity = ChkTags.IsChecked == true ? 1 : 0.45;
    }

    private void Category_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn)
            return;

        _selectedType = btn.Tag?.ToString() switch
        {
            "Top" => ClothingType.Top,
            "Bottom" => ClothingType.Bottom,
            "Outerwear" => ClothingType.Outerwear,
            "Dress" => ClothingType.Dress,
            "Skirt" => ClothingType.Skirt,
            "Shoes" => ClothingType.Shoes,
            "Accessory" => ClothingType.Accessory,
            _ => null
        };
    }

    private void Season_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton btn)
            return;

        _selectedSeason = btn.Tag?.ToString() switch
        {
            "Spring" => Season.Spring,
            "Summer" => Season.Summer,
            "Autumn" => Season.Autumn,
            "Winter" => Season.Winter,
            _ => null
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        var request = new BatchClothingCompletionRequest(
            _clothingIds,
            ChkType.IsChecked == true ? _selectedType : null,
            ChkSeason.IsChecked == true ? _selectedSeason : null,
            ChkColor.IsChecked == true ? TxtColor.Text : null,
            ChkBrand.IsChecked == true ? TxtBrand.Text : null,
            ChkTags.IsChecked == true ? TagSelection.SelectedTags.Select(tag => tag.Id).ToList() : []);

        if (!HasAnyRequestedChange(request))
        {
            await ConfirmModal.ShowMessageAsync(
                "还没有选择补全项",
                "请至少勾选一项要补全的信息。",
                "比如类型、季节、颜色、品牌或标签。选中后这批衣服才知道要统一补什么。",
                confirmText: "继续编辑");
            return;
        }

        _isSubmitting = true;
        BtnSave.IsEnabled = false;
        BtnCancel.IsEnabled = false;
        BtnClose.IsEnabled = false;
        BtnSave.Content = "正在补全...";
        TxtFooterHint.Text = "正在补全当前结果里的缺失资料，请稍等。";

        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchClothingCompletionRequest>(EditorResultType.Saved, request));
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        EditorCompleted?.Invoke(
            this,
            new EditorResult<BatchClothingCompletionRequest>(EditorResultType.Cancelled));
    }

    private static bool HasAnyRequestedChange(BatchClothingCompletionRequest request)
    {
        return request.Type.HasValue ||
            request.Season.HasValue ||
            !string.IsNullOrWhiteSpace(request.Color) ||
            !string.IsNullOrWhiteSpace(request.Brand) ||
            request.TagIds.Count > 0;
    }
}
