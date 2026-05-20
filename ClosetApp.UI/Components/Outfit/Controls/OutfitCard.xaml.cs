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
            var clothes = outfit.OutfitClothes?.Select(oc => oc.Clothing).ToList();
            var chips = BuildMoodChips(outfit, clothes);
            card.TxtName.Text = BuildDisplayName(outfit, clothes);
            card.TxtMoodLine.Text = chips.Count > 0 ? string.Join(" · ", chips.Take(2)) : "今日搭配";
            card.TxtWearInfo.Text = outfit.WearCount > 0
                ? $"穿过 {outfit.WearCount} 次 · 最近 {FormatWornDate(outfit.WornDate)}"
                : "还没记录穿着";
            card.PreviewCanvas.Clothes = clothes;
            card.ApplyPreviewBackdrop(outfit, clothes);
            card.RenderMoodChips(chips);
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

    private static string BuildDisplayName(OutfitEntity outfit, IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var currentName = outfit.Name?.Trim();
        if (!string.IsNullOrWhiteSpace(currentName) &&
            currentName is not "未命名" and not "新搭配" and not "新的搭配")
            return currentName!;

        // 只有占位标题时才兜底，不覆盖用户自己起的名字。
        var tone = ResolveColorTone(clothes);
        var scene = ResolveSceneTitle(outfit.Scene);
        var season = ResolveSeasonTitle(outfit.Season);

        if (!string.IsNullOrWhiteSpace(tone) && !string.IsNullOrWhiteSpace(scene))
            return $"{tone}{scene}";
        if (!string.IsNullOrWhiteSpace(season) && !string.IsNullOrWhiteSpace(scene))
            return $"{season}{scene}";
        if (!string.IsNullOrWhiteSpace(tone))
            return $"{tone}轻搭";
        if (!string.IsNullOrWhiteSpace(scene))
            return $"{scene}穿搭";
        if (!string.IsNullOrWhiteSpace(season))
            return $"{season}轻搭";

        return "今日穿搭";
    }

    private void RenderMoodChips(IReadOnlyList<string> chips)
    {
        MoodChipPanel.Children.Clear();

        foreach (var chip in chips.Take(3))
        {
            var border = new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(28, 217, 162, 153)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(36, 217, 162, 153)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 6)
            };

            border.Child = new TextBlock
            {
                Text = chip,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = global::System.Windows.Application.Current?.TryFindResource("PrimaryBrush") as Brush ?? Brushes.Black
            };

            MoodChipPanel.Children.Add(border);
        }

        MoodChipPanel.Visibility = MoodChipPanel.Children.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
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

    private static IReadOnlyList<string> BuildMoodChips(OutfitEntity outfit, IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var chips = new List<string>();
        var season = ResolveSeasonChip(outfit.Season);
        var tone = ResolveColorTone(clothes);
        var scene = ResolveSceneChip(outfit.Scene);
        var silhouette = ResolveSilhouetteChip(clothes);

        if (!string.IsNullOrWhiteSpace(season))
            chips.Add(season);
        if (!string.IsNullOrWhiteSpace(tone))
            chips.Add(tone);
        if (!string.IsNullOrWhiteSpace(scene))
            chips.Add(scene);
        if (!string.IsNullOrWhiteSpace(silhouette))
            chips.Add(silhouette);

        return chips.Distinct().ToList();
    }

    private static string ResolveSeasonChip(Season season)
    {
        return season switch
        {
            Season.Spring => "春",
            Season.Summer => "夏",
            Season.Autumn => "秋",
            Season.Winter => "冬",
            Season.AllSeason => "四季",
            _ => string.Empty
        };
    }

    private static string ResolveSeasonTitle(Season season)
    {
        return season switch
        {
            Season.Spring => "春日",
            Season.Summer => "夏日",
            Season.Autumn => "秋日",
            Season.Winter => "冬日",
            Season.AllSeason => "四季",
            _ => string.Empty
        };
    }

    private static string ResolveSceneChip(OutfitScene scene)
    {
        return scene switch
        {
            OutfitScene.Work => "通勤",
            OutfitScene.Date => "约会",
            OutfitScene.Travel => "出游",
            OutfitScene.Party => "聚会",
            OutfitScene.Casual => "休闲",
            _ => string.Empty
        };
    }

    private static string ResolveSceneTitle(OutfitScene scene)
    {
        return scene switch
        {
            OutfitScene.Work => "通勤",
            OutfitScene.Date => "约会",
            OutfitScene.Travel => "出游",
            OutfitScene.Party => "派对",
            OutfitScene.Casual => "休闲",
            _ => string.Empty
        };
    }

    private static string? ResolveSilhouetteChip(IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        if (clothes == null || clothes.Count == 0)
            return null;

        bool hasDress = clothes.Any(c => IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Dress, "dress"));
        bool hasSkirt = clothes.Any(c => IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Skirt, "skirt"));
        bool hasOuterwear = clothes.Any(c => IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Outerwear, "coat", "jacket", "cardigan"));

        if (hasDress)
            return "连衣裙";
        if (hasOuterwear)
            return "叠穿";
        if (hasSkirt)
            return "裙装";

        return "轻搭";
    }

    private static string? ResolveColorTone(IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var colorTokens = clothes?
            .Select(c => c.Color?.ToLowerInvariant())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (colorTokens == null || colorTokens.Count == 0)
            return null;

        if (colorTokens.Any(c => c!.Contains("pink") || c.Contains("粉")))
            return "奶油粉";
        if (colorTokens.Any(c => c!.Contains("white") || c.Contains("cream") || c.Contains("白") || c.Contains("米")))
            return "奶油白";
        if (colorTokens.Any(c => c!.Contains("blue") || c.Contains("蓝")))
            return "雾蓝";
        if (colorTokens.Any(c => c!.Contains("green") || c.Contains("绿")))
            return "柔绿";
        if (colorTokens.Any(c => c!.Contains("yellow") || c.Contains("黄")))
            return "奶油黄";
        if (colorTokens.Any(c => c!.Contains("brown") || c.Contains("棕") || c.Contains("咖")))
            return "可可棕";
        if (colorTokens.Any(c => c!.Contains("black") || c.Contains("黑") || c.Contains("gray") || c.Contains("grey") || c.Contains("灰")))
            return "灰调";

        return null;
    }

    private static bool IsType(global::ClosetApp.Domain.Entities.Clothing clothing, global::ClosetApp.Domain.Enums.ClothingType type, params string[] garmentHints)
    {
        if (clothing.Type == type)
            return true;

        var garment = clothing.GarmentType?.ToString();
        if (string.IsNullOrWhiteSpace(garment))
            return false;

        return garmentHints.Any(hint => garment.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }
}
