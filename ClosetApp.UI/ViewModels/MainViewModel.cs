using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using System.Collections.ObjectModel;

namespace ClosetApp.UI.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IClothingService _clothingService;
    private readonly IOutfitService _outfitService;
    private readonly ITagService _tagService;
    private readonly IOutfitRecommendationService _recommendationService;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private ObservableCollection<Clothing> _clothes = new();

    [ObservableProperty]
    private ObservableCollection<Outfit> _outfits = new();

    [ObservableProperty]
    private ObservableCollection<Tag> _tags = new();

    [ObservableProperty]
    private ObservableCollection<RecommendedOutfitDto> _recommendations = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ClothingType? _selectedClothingType;

    [ObservableProperty]
    private bool _isLoading;

    public MainViewModel(
        IClothingService clothingService,
        IOutfitService outfitService,
        ITagService tagService,
        IOutfitRecommendationService recommendationService)
    {
        _clothingService = clothingService;
        _outfitService = outfitService;
        _tagService = tagService;
        _recommendationService = recommendationService;
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var clothesTask = _clothingService.GetAllClothesAsync();
            var outfitsTask = _outfitService.GetAllOutfitsAsync();
            var tagsTask = _tagService.GetAllTagsAsync();

            await Task.WhenAll(clothesTask, outfitsTask, tagsTask);

            Clothes = new ObservableCollection<Clothing>(await clothesTask);
            Outfits = new ObservableCollection<Outfit>(await outfitsTask);
            Tags = new ObservableCollection<Tag>(await tagsTask);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task GetRecommendationsAsync()
    {
        var temperature = 22;
        var recommendations = await _recommendationService.GetRecommendationsByRuleAsync(temperature, null);
        Recommendations = new ObservableCollection<RecommendedOutfitDto>(recommendations);
    }

    [RelayCommand]
    public async Task AddClothingAsync(Clothing clothing)
    {
        await _clothingService.AddClothingAsync(clothing);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task UpdateClothingAsync(Clothing clothing)
    {
        await _clothingService.UpdateClothingAsync(clothing);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task DeleteClothingAsync(Guid id)
    {
        await _clothingService.DeleteClothingAsync(id);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task AddOutfitAsync(Outfit outfit)
    {
        await _outfitService.AddOutfitAsync(outfit);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task UpdateOutfitAsync(Outfit outfit)
    {
        await _outfitService.UpdateOutfitAsync(outfit);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task DeleteOutfitAsync(Guid id)
    {
        await _outfitService.DeleteOutfitAsync(id);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task AddTagAsync(Tag tag)
    {
        await _tagService.AddTagAsync(tag);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task DeleteTagAsync(Guid id)
    {
        await _tagService.DeleteTagAsync(id);
        await LoadDataAsync();
    }

    [RelayCommand]
    public async Task RecordWornAsync(Tuple<Guid, DateTime> param)
    {
        await _outfitService.RecordWornDateAsync(param.Item1, param.Item2);
        await LoadDataAsync();
    }
}
