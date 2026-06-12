using ClosetApp.Domain.Entities;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Logic.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class OutfitsViewModel
{
    public IReadOnlyList<RecentWornListItem> RecentWornRecords => _state.RecentWornRecords;
    public RecentWornListItem? SelectedRecentWornRecord => _state.SelectedRecentWornRecord;
    public IReadOnlyList<CalendarDayItem> CalendarDays => _state.CalendarDays;
    public string HistoryQuickText => _state.HistoryQuickText;
    public string HistorySummaryText => _state.HistorySummaryText;
    public bool IsHistoryExpanded => _state.IsHistoryExpanded;
    public string HistoryToggleText => _state.HistoryToggleText;
    public string CalendarMonthText => _state.CalendarMonthText;
    public string CalendarSummaryText => _state.CalendarSummaryText;
    public bool HasAnyWornRecords => RecentWornRecords.Count > 0;
    public bool HasNoWornRecords => !HasAnyWornRecords;
    public int TodayWornCount => OutfitPresentationText.CountTodayWornRecords(RecentWornRecords);
    public bool HasTodayWornRecords => TodayWornCount > 0;
    public string TodayWornStatusText => OutfitPresentationText.BuildTodayWornStatusText(TodayWornCount);

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

    public async Task FocusHistoryDateAsync(DateTime date)
    {
        var monthChanged = _state.SelectHistoryDate(date);
        if (monthChanged)
            await RefreshCalendarAsync();

        NotifyStateChanged();
    }

    public async Task FocusHistoryRecordAsync(Guid recordId, DateTime date)
    {
        var monthChanged = _state.SelectHistoryRecord(recordId, date);
        if (monthChanged)
            await RefreshCalendarAsync();

        NotifyStateChanged();
    }

    private async Task RefreshDerivedStateAsync()
    {
        var recentRecords = await _outfitService.GetRecentWornRecordsAsync(6);
        _state.SetRecentWornRecords(recentRecords);
    }

    // 首屏先展示搭配列表，历史与日历状态在后台补齐，减少空白等待。
    private async Task RefreshDerivedStateInBackgroundAsync()
    {
        var refreshVersion = Interlocked.Increment(ref _derivedStateRefreshVersion);

        try
        {
            await RefreshDerivedStateAsync();

            if (refreshVersion != _derivedStateRefreshVersion)
                return;

            await RefreshCalendarIfLoadedAsync();

            if (refreshVersion != _derivedStateRefreshVersion)
                return;

            NotifyStateChanged();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Background refresh for outfit history state failed.");
        }
    }

    public async Task EnsureCalendarLoadedAsync()
    {
        if (_state.RecentWornRecords.Count == 0)
            await RefreshDerivedStateAsync();

        if (_state.CalendarDays.Count == 0)
            await RefreshCalendarAsync();

        NotifyStateChanged();
    }

    private async Task RefreshCalendarIfLoadedAsync()
    {
        if (_state.CalendarDays.Count > 0)
            await RefreshCalendarAsync();
    }

    private async Task RefreshCalendarAsync()
    {
        var monthStart = _state.CalendarMonth;
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var monthRecords = await _outfitService.GetWornRecordsAsync(monthStart, monthEnd);
        _state.SetCalendarRecords(monthRecords);
    }
}
