using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel : ObservableObject
{
    private readonly IOutfitService _outfitService;
    private readonly OutfitsTabState _state = new();

    public OutfitsViewModel(IOutfitService outfitService)
    {
        _outfitService = outfitService;
    }

    public IReadOnlyList<Outfit> Outfits => _state.Outfits;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int OutfitCount => _state.Outfits.Count;

    public async Task LoadOutfitsAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            var outfits = await _outfitService.GetAllOutfitsAsync();
            _state.SetOutfits(outfits);
            Log.Debug("Loaded outfits. Count={OutfitCount}", OutfitCount);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public Task RefreshAsync() => LoadOutfitsAsync();

    public Task<IEnumerable<OutfitWornRecord>> GetRecentWornRecordsAsync(int count)
    {
        return _outfitService.GetRecentWornRecordsAsync(count);
    }

    public Task<IEnumerable<OutfitWornRecord>> GetWornRecordsAsync(DateTime start, DateTime end)
    {
        return _outfitService.GetWornRecordsAsync(start, end);
    }

    public async Task DeleteOutfitAsync(Outfit outfit)
    {
        await _outfitService.DeleteOutfitAsync(outfit.Id);
        await LoadOutfitsAsync();
    }

    public async Task RecordWornDateAsync(Outfit outfit, DateTime date)
    {
        await _outfitService.RecordWornDateAsync(outfit.Id, date);
        await LoadOutfitsAsync();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(Outfits));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(OutfitCount));
    }
}
