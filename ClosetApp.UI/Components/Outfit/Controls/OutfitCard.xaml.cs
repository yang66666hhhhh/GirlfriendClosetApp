using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OutfitScene = ClosetApp.Domain.Enums.OutfitScene;
using Season = ClosetApp.Domain.Enums.Season;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Outfit.Controls;

using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;
using ClothingEntity = global::ClosetApp.Domain.Entities.Clothing;

public partial class OutfitCard : UserControl
{
    public static readonly RoutedEvent EditClickedEvent =
        EventManager.RegisterRoutedEvent("EditClicked", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(OutfitCard));

    public static readonly RoutedEvent DeleteClickedEvent =
        EventManager.RegisterRoutedEvent("DeleteClicked", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(OutfitCard));

    public static readonly RoutedEvent WornClickedEvent =
        EventManager.RegisterRoutedEvent("WornClicked", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(OutfitCard));

    public event RoutedEventHandler EditClicked
    {
        add => AddHandler(EditClickedEvent, value);
        remove => RemoveHandler(EditClickedEvent, value);
    }

    public event RoutedEventHandler DeleteClicked
    {
        add => AddHandler(DeleteClickedEvent, value);
        remove => RemoveHandler(DeleteClickedEvent, value);
    }

    public event RoutedEventHandler WornClicked
    {
        add => AddHandler(WornClickedEvent, value);
        remove => RemoveHandler(WornClickedEvent, value);
    }

    public static readonly DependencyProperty OutfitProperty =
        DependencyProperty.Register(
            nameof(Outfit),
            typeof(OutfitEntity),
            typeof(OutfitCard),
            new PropertyMetadata(null, OnOutfitChanged));

    public OutfitEntity? Outfit
    {
        get => (OutfitEntity?)GetValue(OutfitProperty);
        set => SetValue(OutfitProperty, value);
    }

    public OutfitCard()
    {
        InitializeComponent();
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
        BtnEdit.Click += (s, e) =>
        {
            if (Outfit != null)
            {
                EditorModal.Show(new OutfitEditorPanel(Outfit), result =>
                {
                    if (result.Type == EditorResultType.Saved)
                        EditCompleted?.Invoke(this, Outfit);
                    return Task.CompletedTask;
                });
            }
        };
        BtnDelete.Click += async (s, e) =>
        {
            if (Outfit == null) return;
            if (!await ShowDeleteConfirmAsync($"确定删除搭配「{Outfit.Name}」吗？"))
                return;

            DeleteRequested?.Invoke(this, Outfit);
        };
        BtnWorn.Click += (s, e) =>
        {
            RaiseEvent(new RoutedEventArgs(WornClickedEvent, this));
            if (Outfit != null)
                WornRequested?.Invoke(this, Outfit);
        };
    }

    public event EventHandler<OutfitEntity>? EditCompleted;
    public event EventHandler<OutfitEntity>? DeleteRequested;
    public event EventHandler<OutfitEntity>? WornRequested;

    private static void OnOutfitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OutfitCard card && e.NewValue is OutfitEntity outfit)
        {
            card.TxtName.Text = outfit.Name;
            card.TxtSeason.Text = outfit.Season switch
            {
                Season.Spring => "春",
                Season.Summer => "夏",
                Season.Autumn => "秋",
                Season.Winter => "冬",
                Season.AllSeason => "四季",
                _ => ""
            };
            card.TxtWearInfo.Text = outfit.WearCount > 0
                ? $"穿过 {outfit.WearCount} 次 · 最近 {FormatWornDate(outfit.WornDate)}"
                : "还没记录穿着";

            var sceneIcon = card.SceneIcon;
            if (sceneIcon != null)
            {
                card.SceneIcon.Data = outfit.Scene switch
                {
                    OutfitScene.Work => Geometry.Parse("M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm-2 15l-5-5 1.41-1.41L10 14.17l7.59-7.59L19 8l-9 9z"),
                    OutfitScene.Date => Geometry.Parse("M12 21.35l-1.45-1.32C5.4 15.36 2 12.28 2 8.5 2 5.42 4.42 3 7.5 3c1.74 0 3.41.81 4.5 2.09C13.09 3.81 14.76 3 16.5 3 19.58 3 22 5.42 22 8.5c0 3.78-3.4 6.86-8.55 11.54L12 21.35z"),
                    _ => Geometry.Parse("M9 11.75c-.69 0-1.25.56-1.25 1.25s.56 1.25 1.25 1.25 1.25-.56 1.25-1.25-.56-1.25-1.25-1.25zm6 0c-.69 0-1.25.56-1.25 1.25s.56 1.25 1.25 1.25 1.25-.56 1.25-1.25-.56-1.25-1.25-1.25zM12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 18c-4.41 0-8-3.59-8-8 0-.29.02-.58.05-.86 2.36-1.05 4.23-2.98 5.21-5.37C11.07 8.33 14.05 10 17.42 10c.78 0 1.53-.09 2.25-.26.21.71.33 1.47.33 2.26 0 4.41-3.59 8-8 8z")
                };
            }

            var clothes = outfit.OutfitClothes?.Select(oc => oc.Clothing).ToList();
            card.PreviewCanvas.Clothes = clothes;
        }
    }

    private static string FormatWornDate(DateTime? wornDate)
    {
        if (!wornDate.HasValue)
            return "未记录";

        var date = wornDate.Value.Date;
        var today = DateTime.Today;
        if (date == today)
            return "今天";
        if (date == today.AddDays(-1))
            return "昨天";
        return date.ToString("M月d日");
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        var sb = (Storyboard)Resources["CardHoverEnter"];
        sb.Begin();
        CardShadow.BlurRadius = 24;
        ActionOverlay.Visibility = Visibility.Visible;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var sb = (Storyboard)Resources["CardHoverLeave"];
        sb.Begin();
        CardShadow.BlurRadius = 16;
        ActionOverlay.Visibility = Visibility.Collapsed;
    }

    private static async Task<bool> ShowDeleteConfirmAsync(string detail)
    {
        var dialog = new ConfirmDialog
        {
            Title = "确认删除",
            Body = "删除后无法恢复。",
            Detail = detail,
            ConfirmText = "删除",
            CancelText = "取消"
        };

        var tcs = new TaskCompletionSource<bool>();
        void ConfirmedHandler(object? sender, EventArgs e) => tcs.TrySetResult(true);
        void CancelledHandler(object? sender, EventArgs e) => tcs.TrySetResult(false);
        dialog.Confirmed += ConfirmedHandler;
        dialog.Cancelled += CancelledHandler;
        ModalService.Instance.Show(dialog);
        var result = await tcs.Task;
        ModalService.Instance.Hide();
        return result;
    }
}
