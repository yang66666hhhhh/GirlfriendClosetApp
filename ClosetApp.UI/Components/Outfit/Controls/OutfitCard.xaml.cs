using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using OutfitScene = ClosetApp.Domain.Enums.OutfitScene;
using Season = ClosetApp.Domain.Enums.Season;
using ClosetApp.UI.Components.Outfit.Engine;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;

namespace ClosetApp.UI.Components.Outfit.Controls;

using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;

public partial class OutfitCard : UserControl
{
    private static readonly OutfitCompositionEngine PreviewEngine = new();

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
            card.TxtMoodLine.Text = string.Empty;
            card.TxtMoodLine.Visibility = Visibility.Collapsed;
            card.TxtWearInfo.Text = outfit.WearCount > 0
                ? $"穿过 {outfit.WearCount} 次 · 最近 {FormatWornDate(outfit.WornDate)}"
                : "还没记录穿着";
            card.PreviewCanvas.Clothes = clothes;
            card.ApplyPreviewHeight(clothes);
            card.ApplyPreviewBackdrop(outfit, clothes);
            card.RenderMoodChips(chips);
        }
    }

    private void ApplyPreviewHeight(IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        double height = ResolvePreviewHeight(clothes);
        PreviewRow.Height = new GridLength(height);
        PreviewCanvas.Height = Math.Max(240, height - 10);
        PreviewCanvas.MinHeight = Math.Max(240, height - 10);
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

    private static double ResolvePreviewHeight(IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        if (clothes == null || clothes.Count == 0)
            return 312;

        bool hasOuter = clothes.Any(c => IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Outerwear, "coat", "jacket", "cardigan"));
        bool hasShoes = clothes.Any(c => IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Shoes, "shoe", "heels", "sneaker"));
        bool hasBottom = clothes.Any(c =>
            IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Bottom, "pants", "trouser") ||
            IsType(c, global::ClosetApp.Domain.Enums.ClothingType.Skirt, "skirt"));

        var mode = PreviewEngine.DetermineMode(clothes);
        return mode switch
        {
            CompositionMode.Dress => hasOuter ? 370 : hasShoes ? 338 : 316,
            CompositionMode.TopBottom => hasOuter ? 388 : 356,
            CompositionMode.Mixed => hasBottom ? 376 : 336,
            _ => hasShoes ? 296 : 320
        };
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

    private void ApplyPreviewBackdrop(OutfitEntity outfit, IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var backdrop = ResolveBackdrop(outfit, clothes);
        PreviewShell.Background = new SolidColorBrush(backdrop);
    }

    private void RenderMoodChips(IReadOnlyList<string> chips)
    {
        MoodChipPanel.Children.Clear();

        foreach (var chip in chips.Take(4))
        {
            var palette = ResolveChipPalette(chip);
            var border = new Border
            {
                Background = new SolidColorBrush(palette.Background),
                BorderBrush = new SolidColorBrush(palette.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 6)
            };

            border.Child = new TextBlock
            {
                Text = chip,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(palette.Foreground)
            };

            MoodChipPanel.Children.Add(border);
        }

        MoodChipPanel.Visibility = MoodChipPanel.Children.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private (Color Background, Color Border, Color Foreground) ResolveChipPalette(string chip)
    {
        var baseBg = GetThemeColor("PrimaryLightBrush", Color.FromRgb(250, 232, 237));
        var baseBorder = GetThemeColor("BorderLightBrush", Color.FromRgb(240, 228, 224));
        var baseFg = GetThemeColor("PrimaryBrush", Color.FromRgb(218, 148, 165));

        return chip switch
        {
            "春" => (BlendColors(baseBg, Color.FromRgb(255, 235, 230), 0.5), BlendColors(baseBorder, Color.FromRgb(240, 200, 195), 0.3), BlendColors(baseFg, Color.FromRgb(188, 121, 110), 0.4)),
            "夏" => (BlendColors(baseBg, Color.FromRgb(230, 248, 245), 0.5), BlendColors(baseBorder, Color.FromRgb(185, 225, 218), 0.3), BlendColors(baseFg, Color.FromRgb(92, 145, 136), 0.4)),
            "秋" => (BlendColors(baseBg, Color.FromRgb(252, 240, 225), 0.5), BlendColors(baseBorder, Color.FromRgb(235, 208, 178), 0.3), BlendColors(baseFg, Color.FromRgb(176, 122, 79), 0.4)),
            "冬" => (BlendColors(baseBg, Color.FromRgb(235, 238, 248), 0.5), BlendColors(baseBorder, Color.FromRgb(200, 208, 228), 0.3), BlendColors(baseFg, Color.FromRgb(110, 121, 153), 0.4)),
            "四季" => (BlendColors(baseBg, Color.FromRgb(240, 236, 250), 0.5), BlendColors(baseBorder, Color.FromRgb(212, 202, 235), 0.3), BlendColors(baseFg, Color.FromRgb(126, 108, 170), 0.4)),
            "通勤" => (BlendColors(baseBg, Color.FromRgb(248, 240, 230), 0.5), BlendColors(baseBorder, Color.FromRgb(225, 208, 188), 0.3), BlendColors(baseFg, Color.FromRgb(135, 112, 95), 0.4)),
            "约会" => (BlendColors(baseBg, Color.FromRgb(255, 232, 240), 0.4), BlendColors(baseBorder, Color.FromRgb(242, 195, 212), 0.3), BlendColors(baseFg, Color.FromRgb(181, 108, 134), 0.3)),
            "出游" => (BlendColors(baseBg, Color.FromRgb(232, 248, 230), 0.5), BlendColors(baseBorder, Color.FromRgb(195, 225, 185), 0.3), BlendColors(baseFg, Color.FromRgb(104, 145, 92), 0.4)),
            "派对" => (BlendColors(baseBg, Color.FromRgb(245, 232, 248), 0.4), BlendColors(baseBorder, Color.FromRgb(218, 195, 228), 0.3), BlendColors(baseFg, Color.FromRgb(126, 98, 152), 0.4)),
            "休闲" => (BlendColors(baseBg, Color.FromRgb(250, 242, 228), 0.5), BlendColors(baseBorder, Color.FromRgb(230, 215, 185), 0.3), BlendColors(baseFg, Color.FromRgb(150, 120, 88), 0.4)),
            _ => (baseBg, baseBorder, baseFg)
        };
    }

    private static Color ResolveBackdrop(OutfitEntity outfit, IList<global::ClosetApp.Domain.Entities.Clothing>? clothes)
    {
        var baseColor = GetThemeColor("SurfaceHeroBrush", Color.FromRgb(244, 239, 233));

        var colorTokens = clothes?
            .Select(c => c.Color?.ToLowerInvariant())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToList();

        if (colorTokens != null && colorTokens.Count > 0)
        {
            var tint = colorTokens.First() switch
            {
                var c when c!.Contains("pink") || c.Contains("粉") => Color.FromRgb(255, 225, 232),
                var c when c!.Contains("white") || c.Contains("cream") || c.Contains("白") || c.Contains("米") => Color.FromRgb(252, 250, 244),
                var c when c!.Contains("blue") || c.Contains("蓝") => Color.FromRgb(220, 235, 252),
                var c when c!.Contains("green") || c.Contains("绿") => Color.FromRgb(225, 245, 230),
                var c when c!.Contains("yellow") || c.Contains("黄") => Color.FromRgb(252, 248, 220),
                var c when c!.Contains("red") || c.Contains("红") => Color.FromRgb(255, 230, 228),
                var c when c!.Contains("black") || c.Contains("黑") || c.Contains("gray") || c.Contains("grey") || c.Contains("灰") => Color.FromRgb(235, 235, 238),
                var c when c!.Contains("purple") || c.Contains("紫") => Color.FromRgb(240, 232, 248),
                var c when c!.Contains("orange") || c.Contains("橙") || c.Contains("棕") || c.Contains("brown") => Color.FromRgb(250, 238, 225),
                _ => baseColor
            };
            return BlendColors(baseColor, tint, 0.45);
        }

        var seasonTint = outfit.Season switch
        {
            Season.Spring => Color.FromRgb(255, 242, 235),
            Season.Summer => Color.FromRgb(228, 240, 250),
            Season.Autumn => Color.FromRgb(250, 240, 225),
            Season.Winter => Color.FromRgb(232, 235, 245),
            _ => baseColor
        };
        return BlendColors(baseColor, seasonTint, 0.4);
    }

    private static Color GetThemeColor(string key, Color fallback)
    {
        if (global::System.Windows.Application.Current?.TryFindResource(key) is SolidColorBrush brush)
            return brush.Color;
        return fallback;
    }

    private static Color BlendColors(Color a, Color b, double amount)
    {
        byte Lerp(byte x, byte y) => (byte)(x + (y - x) * amount);
        return Color.FromArgb(a.A, Lerp(a.R, b.R), Lerp(a.G, b.G), Lerp(a.B, b.B));
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
