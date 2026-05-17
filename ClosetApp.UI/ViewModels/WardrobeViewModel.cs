using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Infrastructure.Services;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class WardrobeViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ClothesTabState _state = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _isFilterExpanded;

    public IReadOnlyList<Clothing> FilteredClothes => _state.FilteredClothes;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int TotalCount => _state.AllClothes.Count;
    public int FilteredCount => _state.FilteredCount;
    public string FilterSummary => _state.FilterSummary;
    public bool HasActiveFilters => _state.HasActiveFilters;
    public string FilterHint => HasActiveFilters
        ? "当前已应用筛选；点「清除」可以回到完整衣柜。"
        : "选择分类后，衣服列表会立即收窄。";

    public WardrobeViewModel(IClothingService clothingService, IImageStorageService imageStorageService)
    {
        _clothingService = clothingService;
        _imageStorageService = imageStorageService;
    }

    public async Task LoadClothesAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
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

    public void ClearFilters()
    {
        _state.SetSelectedCategories(null);
        SearchText = string.Empty;
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
        OnPropertyChanged(nameof(FilteredClothes));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(TotalCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(FilterSummary));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(FilterHint));
    }
}
