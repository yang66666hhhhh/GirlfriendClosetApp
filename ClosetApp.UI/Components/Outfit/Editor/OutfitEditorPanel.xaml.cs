using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;
using ClothingEntity = ClosetApp.Domain.Entities.Clothing;
using OutfitClothingEntity = ClosetApp.Domain.Entities.OutfitClothing;
using Season = ClosetApp.Domain.Enums.Season;
using OutfitScene = ClosetApp.Domain.Enums.OutfitScene;

namespace ClosetApp.UI.Components.Outfit.Editor;

public partial class OutfitEditorPanel : UserControl
{
    private readonly IClothingService _clothingService;
    private readonly IOutfitService _outfitService;
    private readonly List<SelectableClothing> _allItems = new();
    private readonly bool _isEditMode;
    private readonly OutfitEntity? _existingOutfit;

    public event Action? CloseRequested;
    public event Action? SaveCompleted;

    public OutfitEditorPanel()
    {
        InitializeComponent();
        _clothingService = App.Services.GetRequiredService<IClothingService>();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        CmbScene.SelectedIndex = 4;
        CmbSeason.SelectedIndex = 0;
        Loaded += async (s, e) => await LoadClothesAsync();
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

    private async void OnLoadedForEdit(object s, RoutedEventArgs e)
    {
        await LoadClothesAsync();
    }

    public async Task LoadDataAsync() => await LoadClothesAsync();

    private async Task LoadClothesAsync()
    {
        var clothes = await _clothingService.GetAllClothesAsync();
        _allItems.Clear();
        _allItems.AddRange(clothes.Select(c => new SelectableClothing(c)));

        DressList.ItemsSource = _allItems.Where(c => c.Clothing.Type == ClothingType.Dress).ToList();
        TopsList.ItemsSource = _allItems.Where(c => c.Clothing.Type is ClothingType.Top or ClothingType.Outerwear).ToList();
        BottomsList.ItemsSource = _allItems.Where(c => c.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt).ToList();
        ShoesList.ItemsSource = _allItems.Where(c => c.Clothing.Type == ClothingType.Shoes).ToList();
        AccessoryList.ItemsSource = _allItems.Where(c => c.Clothing.Type == ClothingType.Accessory).ToList();

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

        var type = item.Clothing.Type;

        if (type == ClothingType.Dress)
        {
            ClearGroup(c => c.Clothing.Type is ClothingType.Top or ClothingType.Outerwear or ClothingType.Bottom or ClothingType.Skirt);
            SelectSingle(item, c => c.Clothing.Type == ClothingType.Dress);
        }
        else if (type is ClothingType.Top or ClothingType.Outerwear)
        {
            ClearGroup(c => c.Clothing.Type == ClothingType.Dress);
            SelectSingle(item, c => c.Clothing.Type is ClothingType.Top or ClothingType.Outerwear);
        }
        else if (type is ClothingType.Bottom or ClothingType.Skirt)
        {
            ClearGroup(c => c.Clothing.Type == ClothingType.Dress);
            SelectSingle(item, c => c.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt);
        }
        else if (type == ClothingType.Shoes)
        {
            SelectSingle(item, c => c.Clothing.Type == ClothingType.Shoes);
        }
        else if (type == ClothingType.Accessory)
        {
            SelectSingle(item, c => c.Clothing.Type == ClothingType.Accessory);
        }

        UpdateSectionStates();
        UpdatePreview();
    }

    private void SelectSingle(SelectableClothing target, Func<SelectableClothing, bool> group)
    {
        foreach (var item in _allItems.Where(group))
            item.IsSelected = false;
        target.IsSelected = true;
    }

    private void ClearGroup(Func<SelectableClothing, bool> group)
    {
        foreach (var item in _allItems.Where(group))
            item.IsSelected = false;
    }

    private void UpdateSectionStates()
    {
        bool hasDress = _allItems.Any(i => i.IsSelected && i.Clothing.Type == ClothingType.Dress);
        bool hasTop = _allItems.Any(i => i.IsSelected && i.Clothing.Type is ClothingType.Top or ClothingType.Outerwear);
        bool hasBottom = _allItems.Any(i => i.IsSelected && i.Clothing.Type is ClothingType.Bottom or ClothingType.Skirt);

        TopSection.IsEnabled = !hasDress;
        BottomSection.IsEnabled = !hasDress;
        DressSection.IsEnabled = !hasTop && !hasBottom;

        double disabledOpacity = 0.4;
        TopSection.Opacity = hasDress ? disabledOpacity : 1;
        BottomSection.Opacity = hasDress ? disabledOpacity : 1;
        DressSection.Opacity = (hasTop || hasBottom) ? disabledOpacity : 1;
    }

    private void UpdatePreview()
    {
        var selected = _allItems.Where(i => i.IsSelected).ToList();

        if (selected.Count == 0)
        {
            EmptyPreview.Visibility = Visibility.Visible;
            TxtPreviewCount.Text = "";
            return;
        }

        EmptyPreview.Visibility = Visibility.Collapsed;
        TxtPreviewCount.Text = $"已选 {selected.Count} 件";

        if (LivePreview == null) return;
        LivePreview.Clothes = selected.Select(i => i.Clothing).ToList();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtName.Text))
        {
            MessageBox.Show("请输入搭配名称", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var selectedClothes = _allItems.Where(i => i.IsSelected).Select(i => i.Clothing).ToList();
        if (selectedClothes.Count == 0)
        {
            MessageBox.Show("请至少选择一件衣服", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

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
        }

        TxtName.Text = string.Empty;
        foreach (var item in _allItems) item.IsSelected = false;
        UpdateSectionStates();
        UpdatePreview();

        SaveCompleted?.Invoke();
        CloseRequested?.Invoke();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
    private void Cancel_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
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
