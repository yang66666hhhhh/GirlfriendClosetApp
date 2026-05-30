using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using OutfitScene = ClosetApp.Domain.Enums.OutfitScene;
using Season = ClosetApp.Domain.Enums.Season;
using ClosetApp.UI.Components.Outfit.Editor;
using ClosetApp.UI.Logic.Components.Outfit.Engine;
using ClosetApp.UI.Logic.Components.Outfit.Editor;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;

namespace ClosetApp.UI.Components.Outfit.Controls;

using OutfitEntity = global::ClosetApp.Domain.Entities.Outfit;
using ClothingEntity = global::ClosetApp.Domain.Entities.Clothing;

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

    public static readonly RoutedEvent FavoriteToggledEvent =
        EventManager.RegisterRoutedEvent("FavoriteToggled", RoutingStrategy.Bubble,
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

    public event RoutedEventHandler FavoriteToggled
    {
        add => AddHandler(FavoriteToggledEvent, value);
        remove => RemoveHandler(FavoriteToggledEvent, value);
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
        BtnFavorite.Click += (s, e) =>
        {
            RaiseEvent(new RoutedEventArgs(FavoriteToggledEvent, this));
            e.Handled = true;
        };
    }

    public event EventHandler<OutfitEntity>? EditCompleted;
    public event EventHandler<OutfitEntity>? DeleteRequested;
    public event EventHandler<OutfitEntity>? WornRequested;

    private static void OnOutfitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OutfitCard card && e.NewValue is OutfitEntity outfit)
        {
            var clothes = GetValidClothes(outfit);
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
            card.ApplyFavoriteVisual(outfit);
            card.ApplyChangeWarning(outfit, clothes);
        }
    }

    private void ApplyPreviewHeight(IList<ClothingEntity>? clothes)
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
        AnimateTranslate(-4);
        AnimateScale(1.01);
        AnimateShadow(28, 0.12);
        ActionOverlay.Visibility = Visibility.Visible;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        AnimateTranslate(0);
        AnimateScale(1.0);
        AnimateShadow(16, 0.06);
        ActionOverlay.Visibility = Visibility.Collapsed;
    }

    private void AnimateTranslate(double toY)
    {
        var anim = new DoubleAnimation(toY, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        CardTranslate.BeginAnimation(TranslateTransform.YProperty, anim);
    }

    private void AnimateScale(double to)
    {
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var animX = new DoubleAnimation(to, duration) { EasingFunction = ease };
        var animY = new DoubleAnimation(to, duration) { EasingFunction = ease };
        CardScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        CardScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    private void AnimateShadow(double blur, double opacity)
    {
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var blurAnim = new DoubleAnimation(blur, duration) { EasingFunction = ease };
        var opacityAnim = new DoubleAnimation(opacity, duration) { EasingFunction = ease };
        CardShadow.BeginAnimation(DropShadowEffect.BlurRadiusProperty, blurAnim);
        CardShadow.BeginAnimation(DropShadowEffect.OpacityProperty, opacityAnim);
    }

    private static double ResolvePreviewHeight(IList<ClothingEntity>? clothes)
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

    private static string BuildDisplayName(OutfitEntity outfit, IList<ClothingEntity>? clothes)
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

    private void ApplyPreviewBackdrop(OutfitEntity outfit, IList<ClothingEntity>? clothes)
    {
        var backdrop = ResolveBackdrop(outfit, clothes);
        PreviewShell.Background = new SolidColorBrush(backdrop);
    }

    public void ApplyFavoriteVisual(OutfitEntity outfit)
    {
        var isFav = outfit.Favorites.Count > 0;
        BtnFavorite.Content = isFav ? "♥" : "♡";
        BtnFavorite.Foreground = isFav
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("TextPlaceholderBrush");
        BtnFavorite.Background = isFav
            ? new SolidColorBrush(Color.FromRgb(255, 243, 246))
            : new SolidColorBrush(Color.FromRgb(247, 251, 255));
        BtnFavorite.BorderBrush = isFav
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("BorderLightBrush");
    }

    private void ApplyChangeWarning(OutfitEntity outfit, IList<ClothingEntity>? clothes)
    {
        var currentCount = clothes?.Count ?? 0;
        var originalCount = outfit.OriginalClothingCount;
        var hasChanged = originalCount > 0 && currentCount < originalCount;

        if (hasChanged)
        {
            ChangeWarningBorder.Visibility = Visibility.Visible;
            ChangeWarningText.Text = $"搭配已变化（原 {originalCount} 件，现 {currentCount} 件）";
        }
        else
        {
            ChangeWarningBorder.Visibility = Visibility.Collapsed;
        }
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

    private static (Color Background, Color Border, Color Foreground) ResolveChipPalette(string chip)
    {
        return ThemeColorHelper.ResolveChipPalette(chip);
    }

    private static Color ResolveBackdrop(OutfitEntity outfit, IList<ClothingEntity>? clothes)
    {
        var colors = clothes?.Select(c => c.Color) ?? Enumerable.Empty<string?>();
        return ThemeColorHelper.ResolveOutfitBackdrop(outfit.Season.ToString(), colors);
    }

    private static IReadOnlyList<string> BuildMoodChips(OutfitEntity outfit, IList<ClothingEntity>? clothes)
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

    private static string? ResolveSilhouetteChip(IList<ClothingEntity>? clothes)
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

    private static string? ResolveColorTone(IList<ClothingEntity>? clothes)
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

    private static List<ClothingEntity> GetValidClothes(OutfitEntity outfit)
    {
        return outfit.OutfitClothes?
            .Select(link => link.Clothing)
            .Where(clothing => clothing != null)
            .Cast<ClothingEntity>()
            .ToList() ?? [];
    }

    private static bool IsType(ClothingEntity clothing, global::ClosetApp.Domain.Enums.ClothingType type, params string[] garmentHints)
    {
        if (clothing.Type == type)
            return true;

        var garment = clothing.GarmentType?.ToString();
        if (string.IsNullOrWhiteSpace(garment))
            return false;

        return garmentHints.Any(hint => garment.Contains(hint, StringComparison.OrdinalIgnoreCase));
    }
}
