using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class WardrobeViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly ITagService _tagService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ClothesTabState _state = new();
    private IReadOnlyList<Tag> _availableTags = [];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isFilterExpanded;

    public IReadOnlyList<Tag> AvailableTags => _availableTags;
    public IReadOnlyList<Clothing> FilteredClothes => _state.FilteredClothes;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int TotalCount => _state.AllClothes.Count;
    public int FilteredCount => _state.FilteredCount;
    public string FilterSummary => _state.FilterSummary;
    public bool HasActiveFilters => _state.HasActiveFilters;
    public Season? SelectedSeason => _state.SelectedSeason;
    public IReadOnlyCollection<Guid> SelectedTagIds => _state.SelectedTagIds;
    public bool FavoriteOnly => _state.FavoriteOnly;
    public string FilterHint => HasActiveFilters
        ? "当前已应用组合筛选；点「清除」可以回到完整衣柜。"
        : "分类、季节、标签和收藏都可以叠加筛选。";

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
                _availableTags = (await _tagService.GetStyleTagsAsync()).ToList();

            var clothes = await _clothingService.GetAllClothesAsync();
            _state.SetClothes(clothes);
            Log.Debug("Loaded clothes. Total={TotalCount}, Filtered={FilteredCount}", TotalCount, FilteredCount);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _state.SetSearchText(value);
        NotifyStateChanged();
    }

    public void SetSelectedCategories(IEnumerable<DisplayCategory>? categories)
    {
        _state.SetSelectedCategories(categories);
        NotifyStateChanged();
    }

    public void SetSelectedSeason(Season? season)
    {
        _state.SetSelectedSeason(season);
        NotifyStateChanged();
    }

    public void SetFavoriteOnly(bool favoriteOnly)
    {
        _state.SetFavoriteOnly(favoriteOnly);
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
        NotifyStateChanged();
    }

    public void ClearFilters()
    {
        _state.SetSelectedCategories(null);
        _state.SetSelectedSeason(null);
        _state.SetSelectedTagIds([]);
        _state.SetFavoriteOnly(false);
        SearchText = string.Empty;
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
        OnPropertyChanged(nameof(FilteredClothes));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(FilterSummary));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(SelectedSeason));
        OnPropertyChanged(nameof(SelectedTagIds));
        OnPropertyChanged(nameof(FavoriteOnly));
        OnPropertyChanged(nameof(FilterHint));
    }
}
