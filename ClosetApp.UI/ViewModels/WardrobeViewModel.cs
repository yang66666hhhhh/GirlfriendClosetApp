using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using System.Collections.ObjectModel;

namespace ClosetApp.UI.ViewModels;

public partial class WardrobeViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;

    [ObservableProperty]
    private ObservableCollection<Clothing> _clothes = new();

    [ObservableProperty]
    private ObservableCollection<Clothing> _filteredClothes = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClothingType? _selectedType;

    [ObservableProperty]
    private bool _isLoading;

    public WardrobeViewModel(IClothingService clothingService)
    {
        _clothingService = clothingService;
    }

    [RelayCommand]
    public async Task LoadClothesAsync()
    {
        IsLoading = true;
        try
        {
            var clothes = await _clothingService.GetAllClothesAsync();
            Clothes = new ObservableCollection<Clothing>(clothes);
            ApplyFilter();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnSelectedTypeChanged(ClothingType? value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var filtered = Clothes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedType.HasValue)
        {
            filtered = filtered.Where(c => c.Type == SelectedType.Value);
        }

        FilteredClothes = new ObservableCollection<Clothing>(filtered);
    }

    [RelayCommand]
    public async Task DeleteClothingAsync(Guid id)
    {
        await _clothingService.DeleteClothingAsync(id);
        await LoadClothesAsync();
    }
}