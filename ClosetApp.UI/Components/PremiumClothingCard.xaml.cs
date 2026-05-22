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

    private Point _mouseDownPos;
    private bool _heightApplied;

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

        RenderChips(clothing);
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

    private void CardLoadAnim_Completed(object? sender, EventArgs e) { }

    private void Card_MouseEnter(object sender, MouseEventArgs e)
    {
        AnimateTranslate(-4);
        AnimateScale(1.01);
        AnimateShadow(28, 0.12);
        AnimateImageScale(1.02);
        ActionOverlay.Visibility = Visibility.Visible;
    }

    private void Card_MouseLeave(object sender, MouseEventArgs e)
    {
        AnimateTranslate(0);
        AnimateScale(1.0);
        AnimateShadow(16, 0.06);
        AnimateImageScale(1.0);
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
        _mouseDownPos = e.GetPosition(this);
    }

    private void Card_MouseUp(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _mouseDownPos.X) < 5 && Math.Abs(pos.Y - _mouseDownPos.Y) < 5)
        {
            RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
        }
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
    }

    private void MenuEdit_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(EditClickedEvent, this));
        e.Handled = true;
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        RaiseEvent(new RoutedEventArgs(DeleteClickedEvent, this));
        e.Handled = true;
    }
}
