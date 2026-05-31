using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Services;

namespace ClosetApp.UI.Logic.States;

public enum OutfitSortBy
{
    Newest,
    Oldest,
    Name,
    Rating,
    WearCount,
    LastWorn
}

public sealed class OutfitsTabState
{
    private List<Outfit> _allOutfits = [];
    private List<Outfit> _outfits = [];
    private List<RecentWornListItem> _recentWornRecords = [];
    private List<CalendarDayItem> _calendarDays = [];
    private DateTime _calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selectedHistoryDate;
    private Guid? _selectedHistoryRecordId;
    private bool _isHistoryExpanded;
    private OutfitSortBy _sortBy = OutfitSortBy.Newest;
    private string _searchText = string.Empty;
    private OutfitScene? _selectedScene;
    private Season? _selectedSeason;
    private bool _favoriteOnly;

    public IReadOnlyList<Outfit> Outfits => _outfits;
    public IReadOnlyList<RecentWornListItem> RecentWornRecords => _recentWornRecords;
    public RecentWornListItem? SelectedRecentWornRecord => _recentWornRecords.FirstOrDefault(item => item.IsSelected);
    public IReadOnlyList<CalendarDayItem> CalendarDays => _calendarDays;
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _allOutfits.Count == 0;
    public bool IsFilteredEmpty => _outfits.Count == 0;
    public int OutfitCount => _outfits.Count;
    public int TotalCount => _allOutfits.Count;
    public OutfitSortBy SortBy => _sortBy;
    public string SearchText => _searchText;
    public OutfitScene? SelectedScene => _selectedScene;
    public Season? SelectedSeason => _selectedSeason;
    public bool FavoriteOnly => _favoriteOnly;
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(_searchText) ||
        _selectedScene != null ||
        _selectedSeason != null ||
        _favoriteOnly;
    public string FilterSummary => BuildFilterSummary();
    public DateTime CalendarMonth => _calendarMonth;
    public string CalendarMonthText => _calendarMonth.ToString("yyyy年 M月");
    public bool IsHistoryExpanded => _isHistoryExpanded;
    public string HistoryToggleText => _isHistoryExpanded ? "收起记录日历" : "查看记录日历";
    public string HistoryQuickText => OutfitPresentationText.BuildHistoryQuickText(_recentWornRecords.Count);
    public string HistorySummaryText => OutfitPresentationText.BuildHistorySummaryText(_recentWornRecords.Count);
    public string CalendarSummaryText { get; private set; } = OutfitPresentationText.BuildDefaultCalendarSummaryText();

    public void BeginLoad() => IsLoading = true;

    public void SetOutfits(IEnumerable<Outfit> outfits)
    {
        _allOutfits = outfits.ToList();
        ApplyFilters();
        IsLoading = false;
    }

    public void SetSortBy(OutfitSortBy sortBy)
    {
        _sortBy = sortBy;
        ApplyFilters();
    }

    public void SetSearchText(string? searchText)
    {
        _searchText = searchText?.Trim() ?? string.Empty;
        ApplyFilters();
    }

    public void SetSelectedScene(OutfitScene? scene)
    {
        _selectedScene = scene;
        ApplyFilters();
    }

    public void SetSelectedSeason(Season? season)
    {
        _selectedSeason = season;
        ApplyFilters();
    }

    public void SetFavoriteOnly(bool favoriteOnly)
    {
        _favoriteOnly = favoriteOnly;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        IEnumerable<Outfit> items = _allOutfits;

        if (!string.IsNullOrWhiteSpace(_searchText))
            items = items.Where(MatchesSearch);

        if (_selectedScene != null)
            items = items.Where(outfit => outfit.Scene == _selectedScene.Value);

        if (_selectedSeason != null)
            items = items.Where(MatchesSeasonFilter);

        if (_favoriteOnly)
            items = items.Where(outfit => outfit.Favorites.Count > 0);

        _outfits = ApplySorting(items).ToList();
    }

    private IEnumerable<Outfit> ApplySorting(IEnumerable<Outfit> items)
    {
        return _sortBy switch
        {
            OutfitSortBy.Newest => items.OrderByDescending(o => o.CreatedAt),
            OutfitSortBy.Oldest => items.OrderBy(o => o.CreatedAt),
            OutfitSortBy.Name => items.OrderBy(o => o.Name ?? string.Empty),
            OutfitSortBy.Rating => items.OrderByDescending(o => o.Rating),
            OutfitSortBy.WearCount => items.OrderByDescending(o => o.WearCount),
            OutfitSortBy.LastWorn => items.OrderByDescending(o => o.WornDate ?? o.CreatedAt),
            _ => items.OrderByDescending(o => o.CreatedAt)
        };
    }

    private bool MatchesSearch(Outfit outfit)
    {
        var term = _searchText.Trim();
        if (string.IsNullOrWhiteSpace(term))
            return true;

        var searchableValues = new List<string?>
        {
            outfit.Name,
            outfit.Notes,
            GetSceneLabel(outfit.Scene),
            GetSeasonLabel(outfit.Season)
        };

        searchableValues.AddRange(outfit.OutfitClothes.Select(link => link.Clothing?.Name));
        searchableValues.AddRange(outfit.OutfitClothes.Select(link => link.Clothing?.Brand));
        searchableValues.AddRange(outfit.OutfitClothes.Select(link => link.Clothing?.Color));

        return searchableValues.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private bool MatchesSeasonFilter(Outfit outfit)
    {
        if (_selectedSeason == null)
            return true;

        if (_selectedSeason == Season.AllSeason)
            return outfit.Season == Season.AllSeason;

        return outfit.Season == _selectedSeason.Value || outfit.Season == Season.AllSeason;
    }

    private string BuildFilterSummary()
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(_searchText))
            parts.Add($"搜索「{_searchText}」");

        if (_selectedScene != null)
            parts.Add(GetSceneLabel(_selectedScene.Value));

        if (_selectedSeason != null)
            parts.Add(GetSeasonLabel(_selectedSeason.Value));

        if (_favoriteOnly)
            parts.Add("仅收藏");

        return parts.Count == 0 ? "全部搭配" : string.Join(" + ", parts);
    }

    public void ToggleHistoryExpanded() => _isHistoryExpanded = !_isHistoryExpanded;

    public void MoveCalendarMonth(int offsetMonths)
    {
        _calendarMonth = _calendarMonth.AddMonths(offsetMonths);
    }

    public void SetRecentWornRecords(IEnumerable<OutfitWornRecord> records)
    {
        var items = records
            .Select(RecentWornListItem.FromRecord)
            .ToList();

        if (items.Count == 0)
        {
            _selectedHistoryDate = null;
            _selectedHistoryRecordId = null;
            _recentWornRecords = [];
            return;
        }

        if (_selectedHistoryRecordId is { } selectedRecordId)
        {
            var selectedItem = items.FirstOrDefault(item => item.RecordId == selectedRecordId);
            if (selectedItem != null)
            {
                _selectedHistoryDate = selectedItem.WornDate.Date;
            }
            else
            {
                ResolveSelectionFallback(items, _selectedHistoryDate);
            }
        }
        else
        {
            ResolveSelectionFallback(items, _selectedHistoryDate);
        }

        _recentWornRecords = ApplyRecentSelection(items);
    }

    public void SetCalendarRecords(IEnumerable<OutfitWornRecord> records)
    {
        var monthRecords = records.ToList();
        var groupedRecords = monthRecords
            .GroupBy(record => record.WornDate.Date)
            .ToDictionary(group => group.Key, group => group.ToList());

        CalendarSummaryText = OutfitPresentationText.BuildCalendarSummaryText(monthRecords);
        _calendarDays = BuildCalendarDays(_calendarMonth, groupedRecords, _selectedHistoryDate).ToList();
    }

    public bool SelectHistoryDate(DateTime date)
    {
        _selectedHistoryDate = date.Date;
        _selectedHistoryRecordId = _recentWornRecords
            .FirstOrDefault(item => item.WornDate.Date == _selectedHistoryDate.Value.Date)?.RecordId;
        _recentWornRecords = ApplyRecentSelection(_recentWornRecords);
        _calendarDays = _calendarDays
            .Select(day => day with { IsSelected = day.Date.Date == _selectedHistoryDate.Value.Date })
            .ToList();

        var targetMonth = new DateTime(date.Year, date.Month, 1);
        var monthChanged = targetMonth != _calendarMonth;
        if (monthChanged)
            _calendarMonth = targetMonth;

        return monthChanged;
    }

    public bool SelectHistoryRecord(Guid recordId, DateTime date)
    {
        _selectedHistoryRecordId = recordId;
        _selectedHistoryDate = date.Date;
        _recentWornRecords = ApplyRecentSelection(_recentWornRecords);
        _calendarDays = _calendarDays
            .Select(day => day with { IsSelected = day.Date.Date == _selectedHistoryDate.Value.Date })
            .ToList();

        var targetMonth = new DateTime(date.Year, date.Month, 1);
        var monthChanged = targetMonth != _calendarMonth;
        if (monthChanged)
            _calendarMonth = targetMonth;

        return monthChanged;
    }

    private static string GetSceneLabel(OutfitScene scene) => scene switch
    {
        OutfitScene.Work => "通勤",
        OutfitScene.Date => "约会",
        OutfitScene.Travel => "出游",
        OutfitScene.Party => "派对",
        OutfitScene.Casual => "休闲",
        _ => scene.ToString()
    };

    private static string GetSeasonLabel(Season season) => season switch
    {
        Season.Spring => "春季",
        Season.Summer => "夏季",
        Season.Autumn => "秋季",
        Season.Winter => "冬季",
        Season.AllSeason => "四季",
        _ => "未设季节"
    };

    private static IReadOnlyList<CalendarDayItem> BuildCalendarDays(
        DateTime monthStart,
        IReadOnlyDictionary<DateTime, List<OutfitWornRecord>> recordsByDate,
        DateTime? selectedDate)
    {
        var firstDayOffset = ((int)monthStart.DayOfWeek + 6) % 7;
        var calendarStart = monthStart.AddDays(-firstDayOffset);
        var days = new List<CalendarDayItem>(42);

        for (var index = 0; index < 42; index++)
        {
            var date = calendarStart.AddDays(index);
            recordsByDate.TryGetValue(date, out var dayRecords);
            days.Add(CalendarDayItem.FromDate(date, monthStart.Month, dayRecords ?? [], selectedDate));
        }

        return days;
    }

    private List<RecentWornListItem> ApplyRecentSelection(IEnumerable<RecentWornListItem> items)
    {
        return items
            .Select(item => item with
            {
                IsSelected = _selectedHistoryRecordId != null && item.RecordId == _selectedHistoryRecordId.Value
            })
            .ToList();
    }

    private void ResolveSelectionFallback(IReadOnlyList<RecentWornListItem> items, DateTime? preferredDate)
    {
        var fallbackItem = preferredDate != null
            ? items.FirstOrDefault(item => item.WornDate.Date == preferredDate.Value.Date) ?? items[0]
            : items[0];

        _selectedHistoryRecordId = fallbackItem.RecordId;
        _selectedHistoryDate = fallbackItem.WornDate.Date;
    }
}

public sealed record RecentWornListItem(
    Guid RecordId,
    DateTime WornDate,
    string DateText,
    string OutfitName,
    string TimeText,
    string MetaText,
    IList<Clothing> PreviewClothes,
    string FocusSummaryText,
    string FocusNoteText,
    string FocusSyncText,
    bool IsSelected = false)
{
    public bool HasPreviewClothes => PreviewClothes.Count > 0;

    public static RecentWornListItem FromRecord(OutfitWornRecord record)
    {
        var display = WornRecordSnapshotDisplayFactory.FromRecord(record);
        var date = record.WornDate.Date;
        var dateText = date == DateTime.Today
            ? "今天"
            : date == DateTime.Today.AddDays(-1)
                ? "昨天"
                : date.ToString("M月d日");
        var metaParts = new List<string>();

        if (display.ShouldShowSnapshotStatus && display.SnapshotCount > 0)
            metaParts.Add($"原 {display.SnapshotCount} 件");
        else if (display.PreviewClothes.Count > 0)
            metaParts.Add($"{display.PreviewClothes.Count} 件单品");

        if (record.Outfit?.Season is { } season && season != Season.Unspecified)
            metaParts.Add(GetSeasonLabel(season));

        if (display.ShouldShowSnapshotStatus)
            metaParts.Add(display.IsDeleted ? "搭配已删除" : "搭配已变化");

        var metaText = metaParts.Count > 0
            ? string.Join(" · ", metaParts)
            : "这次穿搭的预览还没补齐";
        var wearSummary = record.Outfit?.WearCount > 0
            ? $"累计穿过 {record.Outfit.WearCount} 次"
            : "这是第一次记进时间线";
        var seasonSummary = record.Outfit?.Season is { } focusSeason && focusSeason != Season.Unspecified
            ? GetSeasonLabel(focusSeason)
            : string.Empty;
        var focusSummaryText = string.IsNullOrWhiteSpace(seasonSummary)
            ? wearSummary
            : $"{wearSummary} · {seasonSummary}";
        var focusNoteText = string.IsNullOrWhiteSpace(record.Outfit?.Notes)
            ? "这套还没有补搭配备注。"
            : record.Outfit!.Notes!.Trim();
        var focusSyncText = $"日历已同步到 {record.WornDate:M月d日}";

        return new RecentWornListItem(
            record.Id,
            record.WornDate,
            dateText,
            display.OutfitName,
            record.WornDate.ToString("HH:mm"),
            metaText,
            display.PreviewClothes,
            focusSummaryText,
            focusNoteText,
            focusSyncText);
    }

    private static string GetSeasonLabel(Season season) => season switch
    {
        Season.Spring => "春季常穿",
        Season.Summer => "夏季常穿",
        Season.Autumn => "秋季常穿",
        Season.Winter => "冬季常穿",
        Season.AllSeason => "四季可穿",
        _ => string.Empty
    };
}

public sealed record CalendarDayItem(
    DateTime Date,
    string DayText,
    string CountText,
    string FirstOutfitName,
    string DensityText,
    string DensitySummaryText,
    IReadOnlyList<OutfitWornRecord> Records,
    int RecordCount,
    bool IsInCurrentMonth,
    bool HasRecords,
    bool HasMultipleRecords,
    bool IsToday,
    bool IsSelected)
{
    public static CalendarDayItem FromDate(
        DateTime date,
        int currentMonth,
        IReadOnlyList<OutfitWornRecord> records,
        DateTime? selectedDate)
    {
        return new CalendarDayItem(
            date,
            date.Day.ToString(),
            records.Count > 0 ? $"{records.Count} 套" : string.Empty,
            BuildFirstOutfitName(records),
            records.Count switch
            {
                0 => string.Empty,
                1 => "单套",
                _ => "多套"
            },
            records.Count switch
            {
                0 => string.Empty,
                1 => "这天只记录了一套",
                _ => $"这天共记录了 {records.Count} 套"
            },
            records,
            records.Count,
            date.Month == currentMonth,
            records.Count > 0,
            records.Count > 1,
            date == DateTime.Today,
            selectedDate != null && date.Date == selectedDate.Value.Date);
    }

    private static string BuildFirstOutfitName(IReadOnlyList<OutfitWornRecord> records)
    {
        if (records.Count == 0)
            return string.Empty;

        var firstRecord = records.FirstOrDefault();
        var firstName = firstRecord == null
            ? string.Empty
            : WornRecordSnapshotDisplayFactory.FromRecord(firstRecord).OutfitName;
        if (records.Count == 1)
            return firstName;

        return string.IsNullOrWhiteSpace(firstName)
            ? $"共 {records.Count} 套"
            : $"{firstName} 等";
    }
}
