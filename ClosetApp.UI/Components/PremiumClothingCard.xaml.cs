using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using ClosetApp.Application.Images;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components;

public partial class PremiumClothingCard : UserControl
{
    private const double ImageStageChromeHeight = 48;
    private const double InfoAreaMinHeight = 80;
    private const double InfoAreaPerChipHeight = 26;
    private const double HoverPopupWidth = 196;
    private const double HoverPopupEdgeGap = 2;
    private static PremiumClothingCard? _activeHoverCard;

    public static readonly RoutedEvent CardClickedEvent = EventManager.RegisterRoutedEvent(
        "CardClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler CardClicked
    {
        add => AddHandler(CardClickedEvent, value);
        remove => RemoveHandler(CardClickedEvent, value);
    }

    public static readonly RoutedEvent EditClickedEvent = EventManager.RegisterRoutedEvent(
        "EditClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler EditClicked
    {
        add => AddHandler(EditClickedEvent, value);
        remove => RemoveHandler(EditClickedEvent, value);
    }

    public static readonly RoutedEvent DeleteClickedEvent = EventManager.RegisterRoutedEvent(
        "DeleteClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler DeleteClicked
    {
        add => AddHandler(DeleteClickedEvent, value);
        remove => RemoveHandler(DeleteClickedEvent, value);
    }

    public static readonly RoutedEvent FavoriteToggledEvent = EventManager.RegisterRoutedEvent(
        "FavoriteToggled", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PremiumClothingCard));

    public event RoutedEventHandler FavoriteToggled
    {
        add => AddHandler(FavoriteToggledEvent, value);
        remove => RemoveHandler(FavoriteToggledEvent, value);
    }

    private Point _mouseDownPos;
    private bool _heightApplied;

    public int LastFavoriteLevelBeforeToggle { get; private set; }

    public PremiumClothingCard()
    {
        InitializeComponent();
        DataContextChanged += (s, e) =>
        {
            if (e.NewValue is global::ClosetApp.Domain.Entities.Clothing)
            {
                _heightApplied = false;
                ApplyCard();
                StartLoadAnimation();
            }
        };
        Unloaded += (_, _) =>
        {
            if (ReferenceEquals(_activeHoverCard, this))
                _activeHoverCard = null;

            CloseHoverUi();
        };
    }

    private void ApplyCard()
    {
        if (DataContext is not global::ClosetApp.Domain.Entities.Clothing c) return;

        CardImage.Stretch = Stretch.Uniform;
        CardImage.Source = ClothingImageLoader.Load(
            c.ImagePath,
            ImageVariant.Display,
            720,
            trimLightPadding: true,
            extractForeground: true);
        ApplyMeta(c);
        ApplyStagePresentation(c);
        ApplyImageBackdrop(c);
        ImageFallback.Visibility = ClothingImageLoader.ResolvePath(c.ImagePath) == null
            ? Visibility.Visible
            : Visibility.Collapsed;

        double colWidth = FindMasonryColumnWidth();
        if (colWidth > 0)
        {
            double imgH = CalcImageHeight(c, colWidth);
            CardImage.Height = imgH;
            double infoH = CalcInfoAreaHeight(c);
            Height = imgH + ImageStageChromeHeight + infoH;
            _heightApplied = true;
        }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (!_heightApplied && DataContext is global::ClosetApp.Domain.Entities.Clothing c)
        {
            double colWidth = FindMasonryColumnWidth();
            if (colWidth > 0)
            {
                double imgH = CalcImageHeight(c, colWidth);
                CardImage.Height = imgH;
                double infoH = CalcInfoAreaHeight(c);
                Height = imgH + ImageStageChromeHeight + infoH;
                _heightApplied = true;
            }
        }
        return base.MeasureOverride(constraint);
    }

    private double FindMasonryColumnWidth()
    {
        var parent = VisualTreeHelper.GetParent(this) as FrameworkElement;
        while (parent != null)
        {
            if (parent is MasonryPanel mp)
                return mp.ColumnWidth;
            parent = VisualTreeHelper.GetParent(parent) as FrameworkElement;
        }
        return 0;
    }

    private double CalcImageHeight(global::ClosetApp.Domain.Entities.Clothing c, double cardWidth)
    {
        string? path = c.ImagePath;
        if (string.IsNullOrEmpty(path))
            return cardWidth * 1.08;

        try
        {
            var imageSize = ClothingImageLoader.GetDisplaySize(
                path,
                trimLightPadding: true);
            if (imageSize == null || imageSize.Value.Width <= 0)
                return cardWidth * 1.08;

            double ratio = imageSize.Value.Height / imageSize.Value.Width;
            ratio = Math.Clamp(ratio, 0.78, 1.35);
            return cardWidth * ratio;
        }
        catch
        {
            return cardWidth * 1.08;
        }
    }

    private double CalcInfoAreaHeight(global::ClosetApp.Domain.Entities.Clothing c)
    {
        double height = InfoAreaMinHeight;

        var chipCount = 0;
        if (c.Season != Season.Unspecified) chipCount++;
        if (c.Type != ClothingType.Unspecified) chipCount++;
        if (!string.IsNullOrWhiteSpace(c.Color)) chipCount++;
        chipCount += c.ClothingTags
            .Count(x => x.Tag?.Category == TagCategory.Style && !string.IsNullOrWhiteSpace(x.Tag.Name));
        chipCount = Math.Min(chipCount, 4);

        if (chipCount > 0)
            height += InfoAreaPerChipHeight;

        if (!string.IsNullOrWhiteSpace(c.Brand?.Trim()))
            height += 16;

        return height;
    }

    private void ApplyImageBackdrop(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        if (string.IsNullOrWhiteSpace(clothing.Color)) return;

        var baseColor = ThemeColorHelper.GetThemeColor("SurfaceHeroBrush", Color.FromRgb(244, 239, 233));
        var imageColor = ThemeColorHelper.GetThemeColor("Surface.ImageArea", Color.FromRgb(248, 241, 236));
        var tint = ThemeColorHelper.ResolveClothingBackdrop(clothing.Color);
        var blended = ThemeColorHelper.Blend(baseColor, tint, 0.5);

        var gradientBrush = new LinearGradientBrush(imageColor, blended, new Point(0, 0), new Point(1, 1));
        ImageAreaBorder.Background = gradientBrush;
    }

    private void ApplyMeta(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var displayName = ResolveDisplayName(clothing);
        var isUnnamed = displayName == "未命名";

        TitleText.Text = displayName;
        TitleText.Foreground = isUnnamed
            ? (Brush)FindResource("TextSecondaryBrush")
            : (Brush)FindResource("TextPrimaryBrush");
        TitleText.FontWeight = isUnnamed ? FontWeights.Medium : FontWeights.SemiBold;

        var brand = clothing.Brand?.Trim();
        MetaLineText.Text = !string.IsNullOrWhiteSpace(brand) ? brand : null;
        MetaLineText.Visibility = string.IsNullOrWhiteSpace(brand)
            ? Visibility.Collapsed
            : Visibility.Visible;

        ApplyFavoriteVisual(clothing);
        RenderChips(clothing);
        ApplyHoverInfo(clothing);
    }

    private void RenderChips(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        ChipPanel.Children.Clear();

        var chips = BuildChipLabels(clothing);
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
                Margin = new Thickness(0, 0, 5, 4)
            };

            border.Child = new TextBlock
            {
                Text = chip,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(palette.Foreground)
            };

            ChipPanel.Children.Add(border);
        }

        ChipPanel.Visibility = ChipPanel.Children.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private static List<string> BuildChipLabels(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var chips = new List<string>();

        if (clothing.Season != Season.Unspecified)
            chips.Add(GetSeasonLabel(clothing.Season));

        if (clothing.Type != ClothingType.Unspecified)
            chips.Add(GetCategoryLabel(clothing.Type));

        if (!string.IsNullOrWhiteSpace(clothing.Color))
            chips.Add(clothing.Color.Trim());

        var styleTags = clothing.ClothingTags
            .Where(x => x.Tag?.Category == TagCategory.Style && !string.IsNullOrWhiteSpace(x.Tag.Name))
            .Select(x => x.Tag.Name.Trim())
            .Distinct();
        chips.AddRange(styleTags);

        return chips.Distinct().Take(4).ToList();
    }

    private static (Color Background, Color Border, Color Foreground) ResolveChipPalette(string chip)
    {
        return ThemeColorHelper.ResolveChipPalette(chip);
    }

    private static string ResolveDisplayName(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var name = clothing.Name?.Trim();
        return string.IsNullOrWhiteSpace(name) ? "未命名" : name;
    }

    private static string GetCategoryLabel(ClothingType type) => type switch
    {
        ClothingType.Unspecified => "分类待补",
        ClothingType.Top => "上衣",
        ClothingType.Bottom => "裤装",
        ClothingType.Dress => "连衣裙",
        ClothingType.Skirt => "半裙",
        ClothingType.Outerwear => "外套",
        ClothingType.Shoes => "鞋子",
        ClothingType.Accessory => "配饰",
        _ => "单品"
    };

    private static string GetSeasonLabel(Season season) => season switch
    {
        Season.Unspecified => "季节待补",
        Season.Spring => "春",
        Season.Summer => "夏",
        Season.Autumn => "秋",
        Season.Winter => "冬",
        Season.AllSeason => "四季",
        _ => "季节"
    };

    private void ApplyStagePresentation(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var category = ClothingMappings.TryGetDisplayCategory(clothing);
        var (scale, offsetY, shadowWidth, shadowOpacity, stageMargin) = category switch
        {
            DisplayCategory.Dress => (0.82, 10.0, 124.0, 0.16, new Thickness(16, 18, 16, 12)),
            DisplayCategory.Topwear when clothing.Type == ClothingType.Outerwear => (0.92, 4.0, 126.0, 0.16, new Thickness(10, 12, 10, 8)),
            DisplayCategory.Topwear => (0.79, -4.0, 110.0, 0.14, new Thickness(16, 18, 16, 12)),
            DisplayCategory.Bottom => (0.86, 8.0, 118.0, 0.15, new Thickness(18, 20, 18, 12)),
            DisplayCategory.Footwear => (0.72, 18.0, 136.0, 0.22, new Thickness(14, 24, 14, 10)),
            DisplayCategory.Accessory => (0.64, 2.0, 92.0, 0.12, new Thickness(20, 20, 20, 14)),
            _ when clothing.Type == ClothingType.Dress => (0.82, 10.0, 124.0, 0.16, new Thickness(16, 18, 16, 12)),
            _ when clothing.Type == ClothingType.Shoes => (0.72, 18.0, 136.0, 0.22, new Thickness(14, 24, 14, 10)),
            _ when clothing.Type == ClothingType.Skirt || clothing.Type == ClothingType.Bottom => (0.86, 8.0, 118.0, 0.15, new Thickness(18, 20, 18, 12)),
            _ when clothing.Type == ClothingType.Outerwear => (0.92, 4.0, 126.0, 0.16, new Thickness(10, 12, 10, 8)),
            _ when clothing.Type == ClothingType.Accessory => (0.64, 2.0, 92.0, 0.12, new Thickness(20, 20, 20, 14)),
            _ => (0.79, -4.0, 110.0, 0.14, new Thickness(16, 18, 16, 12))
        };

        GarmentStage.Margin = stageMargin;
        ImageBaseScale.ScaleX = scale;
        ImageBaseScale.ScaleY = scale;
        ImageTranslate.Y = offsetY;
        GarmentShadow.Width = shadowWidth;
        GarmentShadow.Opacity = shadowOpacity;
    }

    public void StartLoadAnimation()
    {
        var anim = (Storyboard)Resources["CardLoadAnim"];
        anim?.Begin();
    }

    public void RefreshFavoriteVisual()
    {
        if (DataContext is global::ClosetApp.Domain.Entities.Clothing clothing)
            ApplyFavoriteVisual(clothing);
    }

    private void CardLoadAnim_Completed(object? sender, EventArgs e) { }

    private void Card_MouseEnter(object sender, MouseEventArgs e)
    {
        ActivateHoverCard();
        AnimateTranslate(-4);
        AnimateScale(1.01);
        AnimateShadow(28, 0.12);
        AnimateImageScale(1.02);
        BtnMore.Visibility = Visibility.Visible;
        HoverInfoPopup.IsOpen = true;
        PositionHoverPopup(e.GetPosition(CardRoot));
    }

    private void Card_MouseMove(object sender, MouseEventArgs e)
    {
        ActivateHoverCard();

        if (!HoverInfoPopup.IsOpen)
            HoverInfoPopup.IsOpen = true;

        PositionHoverPopup(e.GetPosition(CardRoot));
    }

    private void Card_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimateTranslate(0);
        AnimateScale(1.0);
        AnimateShadow(16, 0.06);
        AnimateImageScale(1.0);
        CloseHoverUi();
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

    private void AnimateImageScale(double to)
    {
        var duration = TimeSpan.FromMilliseconds(220);
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        var animX = new DoubleAnimation(to, duration) { EasingFunction = ease };
        var animY = new DoubleAnimation(to, duration) { EasingFunction = ease };
        ImageScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        ImageScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsInsideCardAction(source))
            return;

        _mouseDownPos = e.GetPosition(this);
    }

    private void Card_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsInsideCardAction(source))
            return;

        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _mouseDownPos.X) < 5 && Math.Abs(pos.Y - _mouseDownPos.Y) < 5)
        {
            RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
        }
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject source && IsInsideCardAction(source))
            return;

        RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not global::ClosetApp.Domain.Entities.Clothing clothing)
            return;

        LastFavoriteLevelBeforeToggle = clothing.FavoriteLevel;
        var isFavorite = clothing.FavoriteLevel >= 4;
        clothing.FavoriteLevel = isFavorite ? 3 : 4;

        ApplyFavoriteVisual(clothing);
        RaiseEvent(new RoutedEventArgs(FavoriteToggledEvent, this));
        e.Handled = true;
    }

    private void MenuEdit_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(EditClickedEvent, this));
        e.Handled = true;
    }

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        if (BtnMore.ContextMenu == null)
            return;

        BtnMore.ContextMenu.PlacementTarget = BtnMore;
        BtnMore.ContextMenu.IsOpen = true;
        e.Handled = true;
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(DeleteClickedEvent, this));
        e.Handled = true;
    }

    private void ApplyFavoriteVisual(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var isFavorite = clothing.FavoriteLevel >= 4;
        BtnFavorite.Content = isFavorite ? "♥" : "♡";
        BtnFavorite.Foreground = isFavorite
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("TextPlaceholderBrush");
        BtnFavorite.Background = isFavorite
            ? new SolidColorBrush(Color.FromRgb(255, 243, 246))
            : new SolidColorBrush(Color.FromRgb(247, 251, 255));
        BtnFavorite.BorderBrush = isFavorite
            ? (Brush)FindResource("DangerBrush")
            : (Brush)FindResource("BorderLightBrush");
    }

    private void ApplyHoverInfo(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        HoverInfoTitleText.Text = BuildHoverTitle(clothing);
        HoverInfoDetailText.Text = BuildHoverDetail(clothing);
        HoverInfoStatusText.Text = BuildStatusText(clothing);
        HoverInfoCreatedText.Text = clothing.CreatedAt.ToString("MM-dd");

        var notes = clothing.Notes?.Trim();
        HoverInfoNotesText.Text = string.IsNullOrWhiteSpace(notes) ? string.Empty : notes;
        HoverInfoNotesCard.Visibility = string.IsNullOrWhiteSpace(notes)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void PositionHoverPopup(Point point)
    {
        var showOnRight = point.X < CardRoot.ActualWidth * 0.5;
        var horizontalOffset = showOnRight
            ? CardRoot.ActualWidth + HoverPopupEdgeGap
            : -HoverPopupWidth - HoverPopupEdgeGap;

        var popupHeight = HoverInfoPopupRoot.ActualHeight;
        if (popupHeight <= 0)
        {
            HoverInfoPopupRoot.Measure(new Size(HoverPopupWidth, double.PositiveInfinity));
            popupHeight = HoverInfoPopupRoot.DesiredSize.Height;
        }

        var maxVertical = Math.Max(14, CardRoot.ActualHeight - popupHeight - 14);
        var verticalOffset = Math.Clamp(point.Y - 10, 14, maxVertical);

        HoverInfoPopup.HorizontalOffset = horizontalOffset;
        HoverInfoPopup.VerticalOffset = verticalOffset;
    }

    private void ActivateHoverCard()
    {
        if (_activeHoverCard != null && !ReferenceEquals(_activeHoverCard, this))
            _activeHoverCard.CloseHoverUi();

        _activeHoverCard = this;
    }

    private void CloseHoverUi()
    {
        BtnMore.Visibility = Visibility.Collapsed;
        HoverInfoPopup.IsOpen = false;

        if (ReferenceEquals(_activeHoverCard, this))
            _activeHoverCard = null;
    }

    private static string BuildStatusText(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var level = Math.Clamp(clothing.FavoriteLevel, 0, 5);
        return level switch
        {
            5 => "已收藏 · 非常喜欢",
            4 => "已收藏 · 常用候选",
            3 => "挺喜欢 · 可以多穿",
            2 => "还不错 · 继续观察",
            1 => "低频保留",
            _ => HasMissingMetadata(clothing) ? "待整理" : "资料完整"
        };
    }

    private static string BuildHoverDetail(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var missing = BuildMissingMetadataParts(clothing);
        if (missing.Count == 0)
        {
            return clothing.FavoriteLevel >= 4
                ? "资料已经完整，又是高偏好单品，后面筛选和搭配会很省心。"
                : "资料已经比较完整，可以直接参与筛选和搭配。";
        }

        var primary = missing.Where(item => item.Priority >= 80).Select(item => item.Label).ToList();
        var secondary = missing.Where(item => item.Priority < 80).Select(item => item.Label).ToList();

        if (primary.Count > 0 && secondary.Count > 0)
            return $"先补 {string.Join("、", primary.Take(2))}，再补 {string.Join("、", secondary.Take(2))} 会更完整。";

        if (primary.Count > 0)
            return $"先补 {string.Join("、", primary.Take(2))}，这会直接影响筛选和搭配准确度。";

        if (clothing.FavoriteLevel >= 4)
            return $"你已经很喜欢它了，顺手补 {string.Join("、", secondary.Take(2))}，以后回看会更顺手。";

        return $"还差 {string.Join("、", secondary.Take(3))}，补完后会更好筛选和搭配。";
    }

    private static string BuildHoverTitle(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var missing = BuildMissingMetadataParts(clothing);
        var missingCount = missing.Count;
        if (missing.Any(item => item.Priority >= 90))
            return "先补基础资料，后面会省很多事";

        return missingCount switch
        {
            0 => "这件已经整理好了",
            1 => "再补一项就很完整",
            <= 3 => "还有几项资料待补",
            _ => "这件还在待整理状态"
        };
    }

    private static bool HasMissingMetadata(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        return BuildMissingMetadataParts(clothing).Count > 0;
    }

    private static List<(string Label, int Priority)> BuildMissingMetadataParts(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var missing = new List<(string Label, int Priority)>();
        var hasStyleTag = clothing.ClothingTags.Any(x =>
            x.Tag?.Category == TagCategory.Style &&
            !string.IsNullOrWhiteSpace(x.Tag.Name));

        if (clothing.Type == ClothingType.Unspecified)
            missing.Add(("分类", 100));

        if (clothing.Season == Season.Unspecified)
            missing.Add(("季节", 90));

        if (!hasStyleTag)
            missing.Add(("风格标签", 70));

        if (string.IsNullOrWhiteSpace(clothing.Color))
            missing.Add(("颜色", 50));

        if (string.IsNullOrWhiteSpace(clothing.Brand))
            missing.Add(("品牌", 40));

        return missing
            .OrderByDescending(item => item.Priority)
            .ToList();
    }

    private bool IsInsideCardAction(DependencyObject source)
    {
        DependencyObject? current = source;
        while (current != null)
        {
            if (ReferenceEquals(current, BtnFavorite) || ReferenceEquals(current, BtnMore))
                return true;

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }
}
