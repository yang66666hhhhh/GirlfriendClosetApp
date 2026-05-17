using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.UI.Components.Tags.Models;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.States;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class WardrobeViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly ITagService _tagService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ClothesTabState _state = new();
    private IReadOnlyList<Tag> _availableTags = [];
    private readonly ObservableCollection<SelectableTag> _tagFilters = [];
    private bool _isSyncingTagFilters;

    private string _searchText = string.Empty;

    private bool _isFilterExpanded;

    public IReadOnlyList<Tag> AvailableTags => _availableTags;
    public ObservableCollection<SelectableTag> TagFilters => _tagFilters;
    public bool HasAvailableTags => _tagFilters.Count > 0;
    public IReadOnlyList<Clothing> FilteredClothes => _state.FilteredClothes;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int TotalCount => _state.AllClothes.Count;
    public int FilteredCount => _state.FilteredCount;
    public DisplayCategory? SelectedCategory => _state.SelectedCategory;
    public string FilterSummary => _state.FilterSummary;
    public string FilterResultText => $"{FilterSummary} · {FilteredCount} 件结果";
    public bool HasActiveFilters => _state.HasActiveFilters;
    public Season? SelectedSeason => _state.SelectedSeason;
    public IReadOnlyCollection<Guid> SelectedTagIds => _state.SelectedTagIds;
    public bool FavoriteOnly
    {
        get => _state.FavoriteOnly;
        set
        {
            if (_state.FavoriteOnly == value)
                return;

            _state.SetFavoriteOnly(value);
            NotifyStateChanged();
        }
    }
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            _state.SetSearchText(value);
            NotifyStateChanged();
        }
    }
    public bool IsFilterExpanded
    {
        get => _isFilterExpanded;
        private set
        {
            if (!SetProperty(ref _isFilterExpanded, value))
                return;

            OnPropertyChanged(nameof(FilterToggleText));
        }
    }
    public string FilterToggleText => IsFilterExpanded ? "收起筛选" : "展开筛选";
    public string FilterHint => HasActiveFilters
        ? "当前已应用组合筛选；点「清除」可以回到完整衣柜。"
        : "分类、季节、标签和收藏都可以叠加筛选。";
    public bool IsCategoryAllSelected
    {
        get => SelectedCategory == null;
        set
        {
            if (value)
                SetSelectedCategory(null);
        }
    }
    public bool IsCategoryTopSelected
    {
        get => SelectedCategory == DisplayCategory.Topwear;
        set
        {
            if (value)
                SetSelectedCategory(DisplayCategory.Topwear);
        }
    }
    public bool IsCategoryBottomSelected
    {
        get => SelectedCategory == DisplayCategory.Bottom;
        set
        {
            if (value)
                SetSelectedCategory(DisplayCategory.Bottom);
        }
    }
    public bool IsCategoryDressSelected
    {
        get => SelectedCategory == DisplayCategory.Dress;
        set
        {
            if (value)
                SetSelectedCategory(DisplayCategory.Dress);
        }
    }
    public bool IsCategoryShoesSelected
    {
        get => SelectedCategory == DisplayCategory.Footwear;
        set
        {
            if (value)
                SetSelectedCategory(DisplayCategory.Footwear);
        }
    }
    public bool IsCategoryAccessorySelected
    {
        get => SelectedCategory == DisplayCategory.Accessory;
        set
        {
            if (value)
                SetSelectedCategory(DisplayCategory.Accessory);
        }
    }
    public bool IsSeasonAllSelected
    {
        get => SelectedSeason == null;
        set
        {
            if (value)
                SetSelectedSeason(null);
        }
    }
    public bool IsSeasonSpringSelected
    {
        get => SelectedSeason == Season.Spring;
        set
        {
            if (value)
                SetSelectedSeason(Season.Spring);
        }
    }
    public bool IsSeasonSummerSelected
    {
        get => SelectedSeason == Season.Summer;
        set
        {
            if (value)
                SetSelectedSeason(Season.Summer);
        }
    }
    public bool IsSeasonAutumnSelected
    {
        get => SelectedSeason == Season.Autumn;
        set
        {
            if (value)
                SetSelectedSeason(Season.Autumn);
        }
    }
    public bool IsSeasonWinterSelected
    {
        get => SelectedSeason == Season.Winter;
        set
        {
            if (value)
                SetSelectedSeason(Season.Winter);
        }
    }

    public WardrobeViewModel(
        IClothingService clothingService,
        ITagService tagService,
        IImageStorageService imageStorageService)
    {
        _clothingService = clothingService;
        _tagService = tagService;
        _imageStorageService = imageStorageService;
    }

    public async Task LoadClothesAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            if (_availableTags.Count == 0)
            {
                _availableTags = (await _tagService.GetStyleTagsAsync()).ToList();
                BuildTagFilters(_availableTags);
            }

            var clothes = await _clothingService.GetAllClothesAsync();
            _state.SetClothes(clothes);
            SyncTagFiltersFromState();
            Log.Debug("Loaded clothes. Total={TotalCount}, Filtered={FilteredCount}", TotalCount, FilteredCount);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public void SetSelectedCategory(DisplayCategory? category)
    {
        _state.SetSelectedCategory(category);
        NotifyStateChanged();
    }

    public void SetSelectedSeason(Season? season)
    {
        _state.SetSelectedSeason(season);
        NotifyStateChanged();
    }

    public void ToggleTag(Guid tagId, bool isSelected)
    {
        var selected = _state.SelectedTagIds.ToHashSet();
        if (isSelected)
            selected.Add(tagId);
        else
            selected.Remove(tagId);

        _state.SetSelectedTagIds(selected);
        SyncTagFiltersFromState();
        NotifyStateChanged();
    }

    public void ClearFilters()
    {
        _state.SetSelectedCategory(null);
        _state.SetSelectedSeason(null);
        _state.SetSelectedTagIds([]);
        _state.SetFavoriteOnly(false);
        SearchText = string.Empty;
        SyncTagFiltersFromState();
        NotifyStateChanged();
    }

    public void ToggleFilterExpanded() => IsFilterExpanded = !IsFilterExpanded;

    public async Task AddClothingAsync(Clothing clothing)
    {
        await _clothingService.AddClothingAsync(clothing);
        await LoadClothesAsync();
    }

    public async Task UpdateClothingAsync(Clothing clothing, string? oldImagePath)
    {
        await _clothingService.UpdateClothingAsync(clothing);
        await DeleteReplacedImageAsync(oldImagePath, clothing.ImagePath);
        await LoadClothesAsync();
    }

    public async Task DeleteClothingAsync(Clothing clothing)
    {
        Log.Information("Deleting clothing {ClothingId} ({ClothingName})", clothing.Id, clothing.Name);
        await _clothingService.DeleteClothingAsync(clothing.Id);
        await DeleteStoredImageAsync(clothing.ImagePath);
        await LoadClothesAsync();
    }

    private async Task DeleteReplacedImageAsync(string? oldImagePath, string? newImagePath)
    {
        if (string.Equals(oldImagePath, newImagePath, StringComparison.OrdinalIgnoreCase))
            return;

        await DeleteStoredImageAsync(oldImagePath);
    }

    private async Task DeleteStoredImageAsync(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return;

        try
        {
            await _imageStorageService.DeleteImageWithThumbnailAsync(imagePath);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to delete stored clothing image {ImagePath}", imagePath);
        }
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(AvailableTags));
        OnPropertyChanged(nameof(TagFilters));
        OnPropertyChanged(nameof(HasAvailableTags));
        OnPropertyChanged(nameof(FilteredClothes));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(SelectedCategory));
        OnPropertyChanged(nameof(FilterSummary));
        OnPropertyChanged(nameof(FilterResultText));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(SelectedSeason));
        OnPropertyChanged(nameof(SelectedTagIds));
        OnPropertyChanged(nameof(FavoriteOnly));
        OnPropertyChanged(nameof(FilterHint));
        OnPropertyChanged(nameof(FilterToggleText));
        OnPropertyChanged(nameof(IsCategoryAllSelected));
        OnPropertyChanged(nameof(IsCategoryTopSelected));
        OnPropertyChanged(nameof(IsCategoryBottomSelected));
        OnPropertyChanged(nameof(IsCategoryDressSelected));
        OnPropertyChanged(nameof(IsCategoryShoesSelected));
        OnPropertyChanged(nameof(IsCategoryAccessorySelected));
        OnPropertyChanged(nameof(IsSeasonAllSelected));
        OnPropertyChanged(nameof(IsSeasonSpringSelected));
        OnPropertyChanged(nameof(IsSeasonSummerSelected));
        OnPropertyChanged(nameof(IsSeasonAutumnSelected));
        OnPropertyChanged(nameof(IsSeasonWinterSelected));
    }

    private void BuildTagFilters(IEnumerable<Tag> tags)
    {
        foreach (var filter in _tagFilters)
            filter.PropertyChanged -= OnTagFilterPropertyChanged;

        _tagFilters.Clear();
        foreach (var tag in tags)
        {
            var filter = new SelectableTag(tag);
            filter.PropertyChanged += OnTagFilterPropertyChanged;
            _tagFilters.Add(filter);
        }

        SyncTagFiltersFromState();
    }

    private void OnTagFilterPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isSyncingTagFilters || e.PropertyName != nameof(SelectableTag.IsSelected))
            return;

        _state.SetSelectedTagIds(_tagFilters.Where(tag => tag.IsSelected).Select(tag => tag.Tag.Id));
        RefreshTagFilterOpacity();
        NotifyStateChanged();
    }

    private void SyncTagFiltersFromState()
    {
        _isSyncingTagFilters = true;
        var selectedIds = _state.SelectedTagIds.ToHashSet();
        foreach (var filter in _tagFilters)
            filter.IsSelected = selectedIds.Contains(filter.Tag.Id);

        RefreshTagFilterOpacity();
        _isSyncingTagFilters = false;
    }

    private void RefreshTagFilterOpacity()
    {
        var count = _tagFilters.Count(tag => tag.IsSelected);
        double opacity = count switch
        {
            <= 5 => 1.0,
            <= 8 => 0.82,
            _ => 0.58
        };

        foreach (var tag in _tagFilters.Where(tag => !tag.IsSelected))
            tag.UnselectedOpacity = opacity;
    }
}
