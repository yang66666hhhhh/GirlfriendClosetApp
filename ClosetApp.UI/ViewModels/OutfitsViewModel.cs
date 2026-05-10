using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using System.Collections.ObjectModel;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel : ObservableObject
{
    private readonly IOutfitService _outfitService;

    [ObservableProperty]
    private ObservableCollection<Outfit> _outfits = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty = true;

    public OutfitsViewModel(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    [RelayCommand]
    public async Task LoadOutfitsAsync()
    {
        IsLoading = true;
        try
        {
            var outfits = await _outfitService.GetAllOutfitsAsync();
            Outfits = new ObservableCollection<Outfit>(outfits);
            IsEmpty = Outfits.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteOutfitAsync(Guid id)
    {
        await _outfitService.DeleteOutfitAsync(id);
        await LoadOutfitsAsync();
    }
}