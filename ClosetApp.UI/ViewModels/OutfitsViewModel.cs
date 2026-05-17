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
    public IReadOnlyList<RecentWornListItem> RecentWornRecords => _state.RecentWornRecords;
    public IReadOnlyList<CalendarDayItem> CalendarDays => _state.CalendarDays;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int OutfitCount => _state.OutfitCount;
    public string OutfitCountText => $"{OutfitCount} 套搭配";
    public string HistoryQuickText => _state.HistoryQuickText;
    public string HistorySummaryText => _state.HistorySummaryText;
    public bool IsHistoryExpanded => _state.IsHistoryExpanded;
    public string HistoryToggleText => _state.HistoryToggleText;
    public string CalendarMonthText => _state.CalendarMonthText;
    public string CalendarSummaryText => _state.CalendarSummaryText;

    public async Task LoadOutfitsAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            var outfits = await _outfitService.GetAllOutfitsAsync();
            _state.SetOutfits(outfits);
            await RefreshDerivedStateAsync();
            Log.Debug("Loaded outfits. Count={OutfitCount}", OutfitCount);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public Task RefreshAsync() => LoadOutfitsAsync();

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

    public void ToggleHistoryExpanded()
    {
        _state.ToggleHistoryExpanded();
        NotifyStateChanged();
    }

    public async Task MoveCalendarMonthAsync(int offsetMonths)
    {
        _state.MoveCalendarMonth(offsetMonths);
        await RefreshCalendarAsync();
        NotifyStateChanged();
    }

    private async Task RefreshDerivedStateAsync()
    {
        var recentRecords = await _outfitService.GetRecentWornRecordsAsync(6);
        _state.SetRecentWornRecords(recentRecords);
        await RefreshCalendarAsync();
    }

    private async Task RefreshCalendarAsync()
    {
        var monthStart = _state.CalendarMonth;
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var monthRecords = await _outfitService.GetWornRecordsAsync(monthStart, monthEnd);
        _state.SetCalendarRecords(monthRecords);
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(Outfits));
        OnPropertyChanged(nameof(RecentWornRecords));
        OnPropertyChanged(nameof(CalendarDays));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(OutfitCount));
        OnPropertyChanged(nameof(OutfitCountText));
        OnPropertyChanged(nameof(HistoryQuickText));
        OnPropertyChanged(nameof(HistorySummaryText));
        OnPropertyChanged(nameof(IsHistoryExpanded));
        OnPropertyChanged(nameof(HistoryToggleText));
        OnPropertyChanged(nameof(CalendarMonthText));
        OnPropertyChanged(nameof(CalendarSummaryText));
    }
}
