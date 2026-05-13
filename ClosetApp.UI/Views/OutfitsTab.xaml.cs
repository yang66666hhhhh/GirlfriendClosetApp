using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Outfit.Controls;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.States;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using OutfitEntity = ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Views;

public partial class OutfitsTab : UserControl
{
    private readonly IOutfitService _outfitService;
    private readonly OutfitsTabState _state = new();
    private DateTime _calendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private bool _isHistoryExpanded;

    public OutfitsTab()
    {
        InitializeComponent();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        Loaded += async (s, e) => await LoadOutfitsAsync();
    }

    private async Task LoadOutfitsAsync()
    {
        _state.BeginLoad();
        var outfits = await _outfitService.GetAllOutfitsAsync();
        _state.SetOutfits(outfits);
        Log.Debug("Loaded outfits. Count={OutfitCount}", _state.Outfits.Count);
        OutfitsList.ItemsSource = _state.Outfits;
        TxtEmpty.Visibility = _state.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        TxtOutfitCount.Text = $"{_state.Outfits.Count} 套搭配";
        await LoadRecentWornRecordsAsync();
        await LoadCalendarAsync();

        _ = Dispatcher.BeginInvoke(() =>
        {
            foreach (var item in OutfitsList.Items)
            {
                var container = OutfitsList.ItemContainerGenerator.ContainerFromItem(item) as ContentPresenter;
                if (container != null)
                {
                    var card = FindVisualChild<OutfitCard>(container);
                    if (card != null)
                    {
                        card.EditCompleted -= OutfitCard_EditCompleted;
                        card.DeleteRequested -= OutfitCard_DeleteRequested;
                        card.WornRequested -= OutfitCard_WornRequested;
                        card.EditCompleted += OutfitCard_EditCompleted;
                        card.DeleteRequested += OutfitCard_DeleteRequested;
                        card.WornRequested += OutfitCard_WornRequested;
                    }
                }
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    public Task RefreshAsync() => LoadOutfitsAsync();

    private void CreateOutfit_Click(object sender, RoutedEventArgs e)
    {
        EditorModal.Show(new OutfitEditorPanel(), async result =>
        {
            if (result.Type == EditorResultType.Saved)
                await LoadOutfitsAsync();
        });
    }

    private async void OutfitCard_EditCompleted(object? sender, OutfitEntity outfit)
    {
        await LoadOutfitsAsync();
    }

    private async void OutfitCard_DeleteRequested(object? sender, OutfitEntity outfit)
    {
        await _outfitService.DeleteOutfitAsync(outfit.Id);
        await LoadOutfitsAsync();
    }

    private async void OutfitCard_WornRequested(object? sender, OutfitEntity outfit)
    {
        await _outfitService.RecordWornDateAsync(outfit.Id, DateTime.Now);
        await LoadOutfitsAsync();
    }

    private async Task LoadRecentWornRecordsAsync()
    {
        var records = (await _outfitService.GetRecentWornRecordsAsync(6))
            .Select(WornRecordListItem.FromRecord)
            .ToList();

        RecentWornList.ItemsSource = records;
        TxtHistoryQuick.Text = records.Count == 0
            ? "暂无记录"
            : $"{records.Count} 条最近记录";
        TxtHistorySummary.Text = records.Count == 0
            ? "记录一次「今天穿了」，这里就会生成你的穿搭时间线。"
            : $"最近 {records.Count} 条穿着记录，点日历日期可以补记或撤销。";
    }

    private async Task LoadCalendarAsync()
    {
        TxtCalendarMonth.Text = _calendarMonth.ToString("yyyy年 M月");

        var monthStart = _calendarMonth;
        var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
        var monthRecords = (await _outfitService.GetWornRecordsAsync(monthStart, monthEnd)).ToList();
        var records = monthRecords
            .GroupBy(r => r.WornDate.Date)
            .ToDictionary(g => g.Key, g => g.ToList());

        TxtCalendarSummary.Text = BuildCalendarSummary(monthRecords);
        CalendarDaysList.ItemsSource = BuildCalendarDays(monthStart, records);
    }

    private static string BuildCalendarSummary(IReadOnlyList<OutfitWornRecord> records)
    {
        if (records.Count == 0)
            return "这个月还没有穿搭记录。点任意一天，可以补记那天穿了什么。";

        var activeDays = records.Select(r => r.WornDate.Date).Distinct().Count();
        var mostWorn = records
            .GroupBy(r => r.Outfit?.Name ?? "未命名搭配")
            .OrderByDescending(g => g.Count())
            .First();

        return $"本月 {records.Count} 次记录 · {activeDays} 天有穿搭 · 最常穿「{mostWorn.Key}」";
    }

    private static IReadOnlyList<CalendarDayItem> BuildCalendarDays(
        DateTime monthStart,
        IReadOnlyDictionary<DateTime, List<OutfitWornRecord>> recordsByDate)
    {
        var firstDayOffset = ((int)monthStart.DayOfWeek + 6) % 7;
        var calendarStart = monthStart.AddDays(-firstDayOffset);
        var days = new List<CalendarDayItem>(42);

        for (var index = 0; index < 42; index++)
        {
            var date = calendarStart.AddDays(index);
            recordsByDate.TryGetValue(date, out var dayRecords);
            days.Add(CalendarDayItem.FromDate(date, monthStart.Month, dayRecords ?? []));
        }

        return days;
    }

    private async void PrevMonth_Click(object sender, RoutedEventArgs e)
    {
        _calendarMonth = _calendarMonth.AddMonths(-1);
        await LoadCalendarAsync();
    }

    private async void NextMonth_Click(object sender, RoutedEventArgs e)
    {
        _calendarMonth = _calendarMonth.AddMonths(1);
        await LoadCalendarAsync();
    }

    private void ToggleHistory_Click(object sender, RoutedEventArgs e)
    {
        _isHistoryExpanded = !_isHistoryExpanded;
        HistoryPanel.Visibility = _isHistoryExpanded ? Visibility.Visible : Visibility.Collapsed;
        ToggleHistoryButton.Content = _isHistoryExpanded ? "收起记录日历" : "查看记录日历";
    }

    private void CalendarDay_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: CalendarDayItem day })
            return;

        var dialog = new WornDayDetailsDialog(day.Date, day.Records);
        dialog.RecordsChanged += async (_, _) => await LoadOutfitsAsync();
        ModalService.Instance.Show(dialog);
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is T result) return result;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private sealed record WornRecordListItem(string DateText, string OutfitName, string TimeText)
    {
        public static WornRecordListItem FromRecord(OutfitWornRecord record)
        {
            var date = record.WornDate.Date;
            var dateText = date == DateTime.Today
                ? "今天"
                : date == DateTime.Today.AddDays(-1)
                    ? "昨天"
                    : date.ToString("M月d日");

            return new WornRecordListItem(
                dateText,
                record.Outfit?.Name ?? "未命名搭配",
                record.WornDate.ToString("HH:mm"));
        }
    }

    private sealed record CalendarDayItem(
        DateTime Date,
        string DayText,
        string CountText,
        string FirstOutfitName,
        IReadOnlyList<OutfitWornRecord> Records,
        Brush Background,
        Brush BorderBrush,
        Brush DayBrush)
    {
        public static CalendarDayItem FromDate(
            DateTime date,
            int currentMonth,
            IReadOnlyList<OutfitWornRecord> records)
        {
            var inMonth = date.Month == currentMonth;
            var hasRecords = records.Count > 0;
            var isToday = date == DateTime.Today;

            var background = hasRecords
                ? new SolidColorBrush(Color.FromRgb(245, 237, 233))
                : new SolidColorBrush(Color.FromRgb(255, 253, 252));
            var border = isToday
                ? new SolidColorBrush(Color.FromRgb(217, 162, 153))
                : new SolidColorBrush(Color.FromRgb(232, 226, 220));
            var dayBrush = inMonth
                ? new SolidColorBrush(Color.FromRgb(45, 42, 38))
                : new SolidColorBrush(Color.FromRgb(200, 192, 184));

            background.Freeze();
            border.Freeze();
            dayBrush.Freeze();

            var firstName = records.FirstOrDefault()?.Outfit?.Name ?? "";
            return new CalendarDayItem(
                date,
                date.Day.ToString(),
                hasRecords ? $"{records.Count} 套" : "",
                firstName,
                records,
                background,
                border,
                dayBrush);
        }
    }
}
