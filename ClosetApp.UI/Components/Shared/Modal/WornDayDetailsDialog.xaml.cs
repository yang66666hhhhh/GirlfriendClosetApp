using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Logic.States;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class WornDayDetailsDialog : UserControl
{
    private readonly IOutfitService _outfitService;
    private readonly DateTime _date;
    private List<OutfitWornRecord> _records;
    private readonly bool _isEmbedded;
    private Guid? _selectedRecordId;
    private Guid? _previewRecordId;

    public WornDayDetailsDialog(DateTime date, IReadOnlyList<OutfitWornRecord> records, bool isEmbedded = false)
    {
        InitializeComponent();
        _outfitService = App.Services.GetRequiredService<IOutfitService>();
        _date = date.Date;
        _records = records.ToList();
        _isEmbedded = isEmbedded;

        Loaded += async (_, _) => await LoadOutfitOptionsAsync();
        Loaded += (_, _) => ApplyMode();

        TitleText.Text = FormatTitle(_date);
        RefreshRecords();
    }

    public event EventHandler? RecordsChanged;
    public event EventHandler? CloseRequested;

    private async Task LoadOutfitOptionsAsync()
    {
        try
        {
            var outfits = (await _outfitService.GetAllOutfitsAsync())
                .OrderByDescending(o => o.WornDate)
                .ThenBy(o => o.Name)
                .ToList();

            OutfitPicker.ItemsSource = outfits;
            if (outfits.Count > 0)
                OutfitPicker.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("搭配选择列表加载失败", $"无法获取可选搭配数据：{ex.Message}");
        }
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

        if (items.Count == 0)
        {
            _selectedRecordId = null;
            _previewRecordId = null;
            RecordsList.SelectedItem = null;
            ClosePreviewPopup();
            return;
        }

        var selectedItem = _selectedRecordId != null
            ? items.FirstOrDefault(item => item.Id == _selectedRecordId.Value)
            : null;
        selectedItem ??= items[0];
        RecordsList.SelectedItem = selectedItem;

        if (_previewRecordId != null && items.All(item => item.Id != _previewRecordId.Value))
        {
            _previewRecordId = null;
            ClosePreviewPopup();
        }
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
        if (_isEmbedded)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        ModalService.Instance.Hide();
    }

    private async void AddRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (OutfitPicker.SelectedItem is not OutfitEntity outfit)
        {
            ToastService.Instance.ShowInfo("先选择一套搭配再添加记录");
            return;
        }

        try
        {
            var recordTime = _date.Date == DateTime.Today
                ? DateTime.Now
                : _date.Date.AddHours(9);

            await _outfitService.RecordWornDateAsync(outfit.Id, recordTime);
            ToastService.Instance.ShowSuccess($"已记录「{outfit.Name}」", $"穿搭记录已添加到 {_date:M月d日}。");
            await ReloadDayRecordsAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError($"记录「{outfit.Name}」失败", $"无法添加到 {_date:M月d日} 的穿搭记录：{ex.Message}");
        }
    }

    private async void DeleteRecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Guid recordId })
            return;

        try
        {
            await _outfitService.DeleteWornRecordAsync(recordId);
            ToastService.Instance.ShowSuccess("已撤销穿搭记录", $"该条记录已从 {_date:M月d日} 中移除。");
            await ReloadDayRecordsAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("撤销穿搭记录失败", $"无法从 {_date:M月d日} 中移除该记录：{ex.Message}");
        }
    }

    private void RecordsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsList.SelectedItem is not WornDayRecordItem item)
            return;

        _selectedRecordId = item.Id;
    }

    private void PreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { DataContext: WornDayRecordItem item } element)
            return;

        _selectedRecordId = item.Id;
        RecordsList.SelectedItem = item;

        if (RecordPreviewPopup.IsOpen && _previewRecordId == item.Id)
        {
            ClosePreviewPopup();
            return;
        }

        _previewRecordId = item.Id;
        PreviewPopupTitleText.Text = item.OutfitName;
        PreviewPopupMetaText.Text = item.PreviewMetaText;

        if (item.HasPreviewClothes)
        {
            PreviewPopupEmptyText.Visibility = Visibility.Collapsed;
            PreviewPopupCanvas.Visibility = Visibility.Visible;
            PreviewPopupCanvas.Clothes = item.PreviewClothes;
            PreviewPopupGlow.Visibility = Visibility.Visible;
            PreviewPopupShadow.Visibility = Visibility.Visible;
        }
        else
        {
            PreviewPopupEmptyText.Text = "这条记录暂时没有可预览的单品";
            PreviewPopupEmptyText.Visibility = Visibility.Visible;
            PreviewPopupCanvas.Visibility = Visibility.Collapsed;
            PreviewPopupCanvas.Clothes = null;
            PreviewPopupGlow.Visibility = Visibility.Collapsed;
            PreviewPopupShadow.Visibility = Visibility.Collapsed;
        }

        RecordPreviewPopup.PlacementTarget = element;
        RecordPreviewPopup.IsOpen = true;
    }

    private void ClosePreviewPopup()
    {
        RecordPreviewPopup.IsOpen = false;
        PreviewPopupCanvas.Clothes = null;
    }

    private sealed record WornDayRecordItem(
        Guid Id,
        string OutfitName,
        string TimeText,
        string PreviewMetaText,
        IList<global::ClosetApp.Domain.Entities.Clothing> PreviewClothes,
        string? StatusText)
    {
        public bool HasPreviewClothes => PreviewClothes.Count > 0;
        public bool HasStatus => !string.IsNullOrEmpty(StatusText);

        public static WornDayRecordItem FromRecord(OutfitWornRecord record)
        {
            var display = WornRecordSnapshotDisplayFactory.FromRecord(record);

            var statusText = (display.IsDeleted, display.IsChanged, display.HasUsableSnapshot) switch
            {
                (true, _, true) => "搭配已删除",
                (true, _, false) => "搭配已删除（快照不完整）",
                (_, true, true) => "搭配已变化",
                (_, true, false) => "搭配已变化（快照不完整）",
                _ => null
            };

            var metaParts = new List<string> { record.WornDate.ToString("HH:mm") };
            if (display.ShouldShowSnapshotStatus && display.SnapshotCount > 0)
                metaParts.Add($"原 {display.SnapshotCount} 件");
            else if (display.PreviewClothes.Count > 0)
                metaParts.Add($"{display.PreviewClothes.Count} 件单品");

            return new WornDayRecordItem(
                record.Id,
                display.OutfitName,
                record.WornDate.ToString("HH:mm"),
                string.Join(" · ", metaParts),
                display.PreviewClothes,
                statusText);
        }
    }

    private void ApplyMode()
    {
        if (!_isEmbedded)
            return;

        RootCard.Width = 428;
        RootCard.MinHeight = 0;
        RootCard.MaxHeight = 640;
        RootCard.HorizontalAlignment = HorizontalAlignment.Right;
        RootCard.VerticalAlignment = VerticalAlignment.Stretch;
        RootCard.Margin = new Thickness(0);
        FooterHost.Visibility = Visibility.Collapsed;
        RecordPreviewPopup.HorizontalOffset = -2;
        RecordPreviewPopup.VerticalOffset = -4;

        SubtitleText.Text = _records.Count == 0
            ? "补记这一天穿了什么。"
            : $"这一天记录了 {_records.Count} 套穿搭，可继续补记或撤销。";
    }
}
