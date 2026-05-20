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

namespace ClosetApp.UI.Components.Outfit.Controls;

using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;

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
            if (!await ConfirmModal.ShowDeleteAsync($"确定删除搭配「{Outfit.Name}」吗？"))
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
            card.TxtMoodLine.Text = BuildMoodLine(outfit);
            card.TxtWearInfo.Text = outfit.WearCount > 0
                ? $"穿过 {outfit.WearCount} 次 · 最近 {FormatWornDate(outfit.WornDate)}"
                : "还没记录穿着";
            var clothes = outfit.OutfitClothes?.Select(oc => oc.Clothing).ToList();
            card.PreviewCanvas.Clothes = clothes;
            card.ApplyPreviewBackdrop(outfit, clothes);
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
        CardShadow.BlurRadius = 20;
        ActionOverlay.Visibility = Visibility.Visible;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        var sb = (Storyboard)Resources["CardHoverLeave"];
        sb.Begin();
        CardShadow.BlurRadius = 14;
        ActionOverlay.Visibility = Visibility.Collapsed;
    }

    private static string BuildMoodLine(OutfitEntity outfit)
    {
        var parts = new List<string>();

        var season = outfit.Season switch
        {
            Season.Spring => "春",
            Season.Summer => "夏",
            Season.Autumn => "秋",
            Season.Winter => "冬",
            Season.AllSeason => "四季",
            _ => string.Empty
        };

        var scene = outfit.Scene switch
        {
            OutfitScene.Work => "通勤",
            OutfitScene.Date => "约会",
            OutfitScene.Travel => "出游",
            OutfitScene.Party => "聚会",
            OutfitScene.Casual => "日常",
            _ => string.Empty
        };

        if (!string.IsNullOrWhiteSpace(season))
            parts.Add(season);
        if (!string.IsNullOrWhiteSpace(scene))
            parts.Add(scene);

        return parts.Count > 0 ? string.Join(" · ", parts) : "今日搭配";
    }

    private void ApplyPreviewBackdrop(OutfitEntity outfit, IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var backdrop = ResolveBackdrop(outfit, clothes);
        PreviewShell.Background = new SolidColorBrush(backdrop);
    }

    private static Color ResolveBackdrop(OutfitEntity outfit, IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var colorTokens = clothes?
            .Select(c => c.Color?.ToLowerInvariant())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (colorTokens != null)
        {
            if (colorTokens.Any(c => c!.Contains("pink") || c.Contains("粉")))
                return Color.FromRgb(246, 229, 231);
            if (colorTokens.Any(c => c!.Contains("white") || c.Contains("cream") || c.Contains("白") || c.Contains("米")))
                return Color.FromRgb(246, 241, 233);
            if (colorTokens.Any(c => c!.Contains("blue") || c.Contains("蓝")))
                return Color.FromRgb(232, 238, 245);
            if (colorTokens.Any(c => c!.Contains("green") || c.Contains("绿")))
                return Color.FromRgb(234, 241, 233);
            if (colorTokens.Any(c => c!.Contains("yellow") || c.Contains("黄")))
                return Color.FromRgb(250, 241, 220);
            if (colorTokens.Any(c => c!.Contains("black") || c.Contains("黑") || c.Contains("gray") || c.Contains("grey") || c.Contains("灰")))
                return Color.FromRgb(239, 235, 230);
        }

        return outfit.Season switch
        {
            Season.Spring => Color.FromRgb(245, 236, 231),
            Season.Summer => Color.FromRgb(237, 242, 244),
            Season.Autumn => Color.FromRgb(243, 235, 225),
            Season.Winter => Color.FromRgb(236, 236, 238),
            _ => Color.FromRgb(244, 239, 233)
        };
    }

}
