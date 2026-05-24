using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.States;

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
    private List<Outfit> _outfits = new();
    private List<RecentWornListItem> _recentWornRecords = [];
    private List<CalendarDayItem> _calendarDays = [];
    private DateTime _calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime? _selectedHistoryDate;
    private Guid? _selectedHistoryRecordId;
    private bool _isHistoryExpanded;
    private OutfitSortBy _sortBy = OutfitSortBy.Newest;

    public IReadOnlyList<Outfit> Outfits => _outfits;
    public IReadOnlyList<RecentWornListItem> RecentWornRecords => _recentWornRecords;
    public RecentWornListItem? SelectedRecentWornRecord => _recentWornRecords.FirstOrDefault(item => item.IsSelected);
    public IReadOnlyList<CalendarDayItem> CalendarDays => _calendarDays;
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _outfits.Count == 0;
    public int OutfitCount => _outfits.Count;
    public OutfitSortBy SortBy => _sortBy;
    public DateTime CalendarMonth => _calendarMonth;
    public string CalendarMonthText => _calendarMonth.ToString("yyyy年 M月");
    public bool IsHistoryExpanded => _isHistoryExpanded;
    public string HistoryToggleText => _isHistoryExpanded ? "收起记录日历" : "查看记录日历";
    public string HistoryQuickText => _recentWornRecords.Count == 0
        ? "暂无记录"
        : $"{_recentWornRecords.Count} 条最近记录";
    public string HistorySummaryText => _recentWornRecords.Count == 0
        ? "记录一次「今天穿了」，这里就会生成你的穿搭时间线。"
        : $"最近 {_recentWornRecords.Count} 条穿着记录，点日历日期可以补记或撤销。";
    public string CalendarSummaryText { get; private set; } = "按月份回看每天穿了哪套，慢慢就会长出你的穿搭习惯。";

    public void BeginLoad() => IsLoading = true;

    public void SetOutfits(IEnumerable<Outfit> outfits)
    {
        _outfits = ApplySorting(outfits).ToList();
        IsLoading = false;
    }

    public void SetSortBy(OutfitSortBy sortBy)
    {
        _sortBy = sortBy;
        _outfits = ApplySorting(_outfits).ToList();
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

        CalendarSummaryText = BuildCalendarSummary(monthRecords);
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

    private static string BuildCalendarSummary(IReadOnlyList<OutfitWornRecord> records)
    {
        if (records.Count == 0)
            return "这个月还没有穿搭记录。点任意一天，可以补记那天穿了什么。";

        var activeDays = records.Select(record => record.WornDate.Date).Distinct().Count();
        var mostWorn = records
            .GroupBy(record => record.Outfit?.Name ?? "未命名搭配")
            .OrderByDescending(group => group.Count())
            .First();

        return $"本月 {records.Count} 次记录 · {activeDays} 天有穿搭 · 最常穿「{mostWorn.Key}」";
    }

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
        var date = record.WornDate.Date;
        var dateText = date == DateTime.Today
            ? "今天"
            : date == DateTime.Today.AddDays(-1)
                ? "昨天"
                : date.ToString("M月d日");
        var previewClothes = record.Outfit?.OutfitClothes
            .Select(link => link.Clothing)
            .Where(clothing => clothing != null)
            .Cast<Clothing>()
            .ToList() ?? [];
        var metaParts = new List<string>();

        if (previewClothes.Count > 0)
            metaParts.Add($"{previewClothes.Count} 件单品");

        if (record.Outfit?.Season is { } season && season != Season.Unspecified)
            metaParts.Add(GetSeasonLabel(season));

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
            ResolveOutfitName(record.Outfit),
            record.WornDate.ToString("HH:mm"),
            metaText,
            previewClothes,
            focusSummaryText,
            focusNoteText,
            focusSyncText);
    }

    private static string ResolveOutfitName(Outfit? outfit)
    {
        var name = outfit?.Name?.Trim();
        return string.IsNullOrWhiteSpace(name) ? "未命名搭配" : name;
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

        var firstName = records.FirstOrDefault()?.Outfit?.Name ?? string.Empty;
        if (records.Count == 1)
            return firstName;

        return string.IsNullOrWhiteSpace(firstName)
            ? $"共 {records.Count} 套"
            : $"{firstName} 等";
    }
}
