using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;
using ClothingEntity = ClosetApp.Domain.Entities.Clothing;
using OutfitClothingEntity = ClosetApp.Domain.Entities.OutfitClothing;
using Season = ClosetApp.Domain.Enums.Season;
using OutfitScene = ClosetApp.Domain.Enums.OutfitScene;

namespace ClosetApp.UI.Components.Outfit.Editor;

public partial class OutfitEditorPanel : UserControl, IEditorPanel<OutfitEntity>
{
    private readonly IClothingService _clothingService;
    private readonly IOutfitService _outfitService;
    private readonly List<SelectableClothing> _allItems = new();
    private readonly bool _isEditMode;
    private readonly OutfitEntity? _existingOutfit;
    private bool _isSubmitting;

    public event EventHandler<EditorResult<OutfitEntity>>? EditorCompleted;

    public OutfitEditorPanel()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        CmbScene.SelectedIndex = 4;
        CmbSeason.SelectedIndex = 0;
        Loaded += async (s, e) =>
        {
            UpdateCardClip();
            await LoadClothesAsync();
        };
    }

    public OutfitEditorPanel(OutfitEntity outfit) : this()
    {
        _isEditMode = true;
        _existingOutfit = outfit;
        TxtHeader.Text = "编辑搭配";
        BtnSave.Content = "保存修改";
        TxtName.Text = outfit.Name;
        RatingControl.Value = outfit.Rating;

        CmbScene.SelectedIndex = outfit.Scene switch
        {
            OutfitScene.Work => 0,
            OutfitScene.Date => 1,
            OutfitScene.Travel => 2,
            OutfitScene.Party => 3,
            _ => 4
        };
        CmbSeason.SelectedIndex = outfit.Season switch
        {
            Season.Spring => 0,
            Season.Summer => 1,
            Season.Autumn => 2,
            Season.Winter => 3,
            _ => 4
        };

        Loaded -= OnLoadedForEdit;
        Loaded += OnLoadedForEdit;
    }

    private static bool IsTop(ClothingEntity clothing)
    {
        return OutfitSelectionRules.GetSlot(clothing) == OutfitSelectionSlot.Top;
    }

    private static bool IsOuterwear(ClothingEntity clothing)
    {
        return OutfitSelectionRules.GetSlot(clothing) == OutfitSelectionSlot.Outerwear;
    }

    private static bool IsPants(ClothingEntity clothing)
    {
        return OutfitSelectionRules.GetSlot(clothing) == OutfitSelectionSlot.LowerBody && !IsSkirt(clothing);
    }

    private static bool IsSkirt(ClothingEntity clothing)
    {
        return clothing.Type == ClothingType.Skirt || clothing.GarmentType == GarmentType.Skirt;
    }

    private static bool IsLowerBody(ClothingEntity clothing)
    {
        return OutfitSelectionRules.GetSlot(clothing) == OutfitSelectionSlot.LowerBody;
    }

    private async void OnLoadedForEdit(object s, RoutedEventArgs e)
    {
        await LoadClothesAsync();
    }

    public async Task LoadDataAsync() => await LoadClothesAsync();

    private void CardClip_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateCardClip();

    private void UpdateCardClip()
    {
        if (CardClip.ActualWidth <= 0 || CardClip.ActualHeight <= 0)
            return;

        CardClip.Clip = new RectangleGeometry(
            new Rect(0, 0, CardClip.ActualWidth, CardClip.ActualHeight),
            24,
            24);
    }

    private async Task LoadClothesAsync()
    {
        var clothes = await _clothingService.GetAllClothesAsync();
        _allItems.Clear();
        _allItems.AddRange(clothes.Select(c => new SelectableClothing(c)));

        DressList.ItemsSource = _allItems.Where(c => OutfitSelectionRules.GetSlot(c.Clothing) == OutfitSelectionSlot.Dress).ToList();
        TopsList.ItemsSource = _allItems.Where(c => IsTop(c.Clothing)).ToList();
        OuterwearList.ItemsSource = _allItems.Where(c => IsOuterwear(c.Clothing)).ToList();
        BottomsList.ItemsSource = _allItems.Where(c => IsPants(c.Clothing)).ToList();
        SkirtsList.ItemsSource = _allItems.Where(c => IsSkirt(c.Clothing)).ToList();
        ShoesList.ItemsSource = _allItems.Where(c => OutfitSelectionRules.GetSlot(c.Clothing) == OutfitSelectionSlot.Footwear).ToList();
        AccessoryList.ItemsSource = _allItems.Where(c => OutfitSelectionRules.GetSlot(c.Clothing) == OutfitSelectionSlot.Accessory).ToList();

        if (_isEditMode && _existingOutfit != null)
        {
            var existingIds = _existingOutfit.OutfitClothes
                .Select(oc => oc.ClothingId).ToHashSet();
            foreach (var item in _allItems.Where(i => existingIds.Contains(i.Clothing.Id)))
                item.IsSelected = true;
        }
        UpdatePreview();
    }

    private void ClothItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.DataContext is not SelectableClothing item) return;

        if (item.IsSelected)
        {
            item.IsSelected = false;
            UpdateSectionStates();
            UpdatePreview();
            return;
        }

        var slot = OutfitSelectionRules.GetSlot(item.Clothing);
        if (slot == OutfitSelectionSlot.Unknown)
            return;

        if (slot == OutfitSelectionSlot.Accessory)
        {
            item.IsSelected = true;
        }
        else
        {
            ClearGroup(candidate => OutfitSelectionRules.ShouldClearWhenSelecting(item.Clothing, candidate.Clothing));
            item.IsSelected = true;
        }

        UpdateSectionStates();
        UpdatePreview();
    }

    private void ClearGroup(Func<SelectableClothing, bool> group)
    {
        foreach (var i in _allItems.Where(group))
            i.IsSelected = false;
    }

    private void UpdateSectionStates()
    {
        var selected = _allItems.Where(i => i.IsSelected).Select(i => i.Clothing).ToList();
        bool hasFullBody = OutfitSelectionRules.DisablesTopOrLowerBody(selected);
        bool disablesDress = OutfitSelectionRules.DisablesDress(selected);

        TopSection.IsEnabled = !hasFullBody;
        OuterwearSection.IsEnabled = true;
        BottomSection.IsEnabled = !hasFullBody;
        SkirtSection.IsEnabled = !hasFullBody;
        DressSection.IsEnabled = !disablesDress;

        double disabledOpacity = 0.4;
        TopSection.Opacity = hasFullBody ? disabledOpacity : 1;
        OuterwearSection.Opacity = 1;
        BottomSection.Opacity = hasFullBody ? disabledOpacity : 1;
        SkirtSection.Opacity = hasFullBody ? disabledOpacity : 1;
        DressSection.Opacity = disablesDress ? disabledOpacity : 1;
    }

    private void UpdatePreview()
    {
        var selected = _allItems.Where(i => i.IsSelected).ToList();

        if (selected.Count == 0)
        {
            EmptyPreview.Visibility = Visibility.Visible;
            TxtPreviewCount.Text = "";
            LivePreview.Clothes = null;
            return;
        }

        EmptyPreview.Visibility = Visibility.Collapsed;
        TxtPreviewCount.Text = $"已选 {selected.Count} 件";

        if (LivePreview == null) return;
        LivePreview.Clothes = null;
        LivePreview.Clothes = selected.Select(i => i.Clothing).ToList();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting)
            return;

        var selectedClothes = _allItems.Where(i => i.IsSelected).Select(i => i.Clothing).ToList();
        if (selectedClothes.Count == 0)
        {
            ToastService.Instance.ShowInfo("请至少选择一件衣服。");
            return;
        }

        try
        {
            SetSubmitting(true);

            var scene = (CmbScene.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
            {
                "Work" => OutfitScene.Work, "Date" => OutfitScene.Date,
                "Travel" => OutfitScene.Travel, "Party" => OutfitScene.Party,
                _ => OutfitScene.Casual
            };
            var season = (CmbSeason.SelectedItem as ComboBoxItem)?.Tag?.ToString() switch
            {
                "Spring" => Season.Spring, "Summer" => Season.Summer,
                "Autumn" => Season.Autumn, "Winter" => Season.Winter,
                _ => Season.AllSeason
            };

            OutfitEntity savedOutfit;
            if (_isEditMode && _existingOutfit != null)
            {
                _existingOutfit.Name = TxtName.Text.Trim();
                _existingOutfit.Scene = scene;
                _existingOutfit.Season = season;
                _existingOutfit.Rating = (int)RatingControl.Value;
                _existingOutfit.OutfitClothes.Clear();
                foreach (var clothing in selectedClothes)
                    _existingOutfit.OutfitClothes.Add(new OutfitClothingEntity { OutfitId = _existingOutfit.Id, ClothingId = clothing.Id });
                await _outfitService.UpdateOutfitAsync(_existingOutfit);
                savedOutfit = _existingOutfit;
            }
            else
            {
                var outfit = new OutfitEntity
                {
                    Name = TxtName.Text.Trim(),
                    Scene = scene,
                    Season = season,
                    Rating = (int)RatingControl.Value
                };
                await _outfitService.AddOutfitAsync(outfit);
                foreach (var clothing in selectedClothes)
                    outfit.OutfitClothes.Add(new OutfitClothingEntity { OutfitId = outfit.Id, ClothingId = clothing.Id });
                await _outfitService.UpdateOutfitAsync(outfit);
                savedOutfit = outfit;
            }

            TxtName.Text = string.Empty;
            foreach (var item in _allItems) item.IsSelected = false;
            UpdateSectionStates();
            UpdatePreview();

            EditorCompleted?.Invoke(this, new EditorResult<OutfitEntity>(EditorResultType.Saved, savedOutfit));
        }
        catch (Exception ex)
        {
            SetSubmitting(false);
            ToastService.Instance.ShowError(_isEditMode ? "保存搭配失败" : "创建搭配失败", ex.Message);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) =>
        EditorCompleted?.Invoke(this, new EditorResult<OutfitEntity>(EditorResultType.Cancelled));

    private void Cancel_Click(object sender, RoutedEventArgs e) =>
        EditorCompleted?.Invoke(this, new EditorResult<OutfitEntity>(EditorResultType.Cancelled));

    private void SetSubmitting(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        BtnSave.IsEnabled = !isSubmitting;
        BtnCancel.IsEnabled = !isSubmitting;
        BtnClose.IsEnabled = !isSubmitting;
        BtnSave.Content = isSubmitting
            ? (_isEditMode ? "正在保存..." : "正在创建...")
            : (_isEditMode ? "保存修改" : "创建搭配");
    }
}

public class SelectableClothing : System.ComponentModel.INotifyPropertyChanged
{
    public ClothingEntity Clothing { get; }
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set { if (_isSelected != value) { _isSelected = value; PropertyChanged?.Invoke(this, new(nameof(IsSelected))); } }
    }
    public string? ImagePath => Clothing.ImagePath;
    public SelectableClothing(ClothingEntity clothing) => Clothing = clothing;
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
