using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class WornDayDetailsDialog : UserControl
{
    private readonly IOutfitService _outfitService;
    private readonly DateTime _date;
    private List<OutfitWornRecord> _records;

    public WornDayDetailsDialog(DateTime date, IReadOnlyList<OutfitWornRecord> records)
    {
        InitializeComponent();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        _date = date.Date;
        _records = records.ToList();

        Loaded += async (_, _) => await LoadOutfitOptionsAsync();

        TitleText.Text = FormatTitle(_date);
        RefreshRecords();
    }

    public event EventHandler? RecordsChanged;

    private async Task LoadOutfitOptionsAsync()
    {
        var outfits = (await _outfitService.GetAllOutfitsAsync())
            .OrderByDescending(o => o.WornDate)
            .ThenBy(o => o.Name)
            .ToList();

        OutfitPicker.ItemsSource = outfits;
        if (outfits.Count > 0)
            OutfitPicker.SelectedIndex = 0;
    }

    private void RefreshRecords()
    {
        SubtitleText.Text = _records.Count == 0
            ? "这一天还没有记录穿搭"
            : $"这一天记录了 {_records.Count} 套穿搭";

        var items = _records
            .OrderByDescending(r => r.WornDate)
            .Select(WornDayRecordItem.FromRecord)
            .ToList();

        RecordsList.ItemsSource = items;
        EmptyText.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task ReloadDayRecordsAsync()
    {
        var start = _date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        _records = (await _outfitService.GetWornRecordsAsync(start, end)).ToList();
        RefreshRecords();
        RecordsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static string FormatTitle(DateTime date)
    {
        if (date.Date == DateTime.Today)
            return "今天的穿搭";
        if (date.Date == DateTime.Today.AddDays(-1))
            return "昨天的穿搭";
        return date.ToString("yyyy年 M月 d日");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private async void AddRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (OutfitPicker.SelectedItem is not OutfitEntity outfit)
        {
            ToastService.Instance.ShowInfo("先选择一套搭配再添加记录");
            return;
        }

        var recordTime = _date.Date == DateTime.Today
            ? DateTime.Now
            : _date.Date.AddHours(9);

        await _outfitService.RecordWornDateAsync(outfit.Id, recordTime);
        ToastService.Instance.ShowSuccess("已添加穿搭记录");
        await ReloadDayRecordsAsync();
    }

    private async void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid recordId })
            return;

        await _outfitService.DeleteWornRecordAsync(recordId);
        ToastService.Instance.ShowSuccess("已撤销这条记录");
        await ReloadDayRecordsAsync();
    }

    private sealed record WornDayRecordItem(Guid Id, string OutfitName, string TimeText)
    {
        public static WornDayRecordItem FromRecord(OutfitWornRecord record)
        {
            return new WornDayRecordItem(
                record.Id,
                record.Outfit?.Name ?? "未命名搭配",
                record.WornDate.ToString("HH:mm"));
        }
    }
}
