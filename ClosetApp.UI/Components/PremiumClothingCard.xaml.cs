using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ClosetApp.Application.Images;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components;

public partial class PremiumClothingCard : UserControl
{
    private const double ImageStageChromeHeight = 48;
    private const double InfoAreaHeight = 68;

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

    private bool _isMenuOpen;
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

        var useTrimmedPresentation = ShouldTrimLightPadding(c);

        CardImage.Stretch = Stretch.Uniform;
        CardImage.Source = ClothingImageLoader.Load(
            c.ImagePath,
            ImageVariant.Display,
            720,
            useTrimmedPresentation);
        ApplyMeta(c);
        ApplyStagePresentation(c);
        ImageFallback.Visibility = ClothingImageLoader.ResolvePath(c.ImagePath) == null
            ? Visibility.Visible
            : Visibility.Collapsed;

        double colWidth = FindMasonryColumnWidth();
        if (colWidth > 0)
        {
            double imgH = CalcImageHeight(c, colWidth);
            CardImage.Height = imgH;
            Height = imgH + ImageStageChromeHeight + InfoAreaHeight;
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
                Height = imgH + ImageStageChromeHeight + InfoAreaHeight;
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
                trimLightPadding: ShouldTrimLightPadding(c));
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

    // Tops and outerwear are easy to over-trim into awkward silhouettes, so keep them conservative.
    private static bool ShouldTrimLightPadding(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        return clothing.Type switch
        {
            ClothingType.Top => false,
            ClothingType.Outerwear => false,
            ClothingType.Accessory => false,
            _ => true
        };
    }

    // Build a lightweight secondary line so the card reads like a finished item card.
    private void ApplyMeta(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        var displayName = ResolveDisplayName(clothing);
        var isUnnamed = displayName == "未命名";
        var parts = new List<string>();
        var styleTags = clothing.ClothingTags
            .Where(x => x.Tag?.Category == TagCategory.Style && !string.IsNullOrWhiteSpace(x.Tag.Name))
            .Select(x => x.Tag.Name.Trim())
            .Distinct()
            .Take(3)
            .ToList();
        var hasStyleTags = styleTags.Count > 0;

        TitleText.Text = displayName;
        TitleText.Foreground = isUnnamed
            ? (Brush)FindResource("TextSecondaryBrush")
            : (Brush)FindResource("TextPrimaryBrush");
        TitleText.FontWeight = isUnnamed ? FontWeights.Medium : FontWeights.SemiBold;

        CategoryChipText.Text = GetCategoryLabel(clothing.Type);
        SeasonChipText.Text = GetSeasonLabel(clothing.Season);
        ApplyChipTone(
            CategoryChip,
            CategoryChipText,
            clothing.Type == ClothingType.Unspecified,
            "#FFF8F5F2",
            "#FFF7F3EF",
            "#FFE7DDD4",
            "TextTertiaryBrush",
            "TextSecondaryBrush");
        ApplyChipTone(
            SeasonChip,
            SeasonChipText,
            clothing.Season == Season.Unspecified,
            "#FFF8F5F2",
            "#FFF9F1EE",
            "#FFE8D5CF",
            "TextTertiaryBrush",
            null);

        if (styleTags.Count > 0)
        {
            MetaLineText.Text = string.Join(" · ", styleTags);
            MetaLineText.Visibility = Visibility.Visible;
            CategoryChip.Visibility = Visibility.Collapsed;
            SeasonChip.Visibility = clothing.Season == Season.Unspecified
                ? Visibility.Collapsed
                : Visibility.Visible;
            return;
        }

        if (!string.IsNullOrWhiteSpace(clothing.Color))
            parts.Add(clothing.Color.Trim());

        if (!string.IsNullOrWhiteSpace(clothing.Brand))
            parts.Add(clothing.Brand.Trim());

        var hasSupportMeta = parts.Count > 0;
        var showCategoryChip = !hasStyleTags && (clothing.Type != ClothingType.Unspecified || !hasSupportMeta);
        var showSeasonChip = clothing.Season != Season.Unspecified || (!hasStyleTags && !hasSupportMeta);

        MetaLineText.Text = string.Join(" · ", parts);
        MetaLineText.Visibility = hasSupportMeta ? Visibility.Visible : Visibility.Collapsed;
        CategoryChip.Visibility = showCategoryChip ? Visibility.Visible : Visibility.Collapsed;
        SeasonChip.Visibility = showSeasonChip ? Visibility.Visible : Visibility.Collapsed;
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

    private void ApplyChipTone(
        Border chip,
        TextBlock text,
        bool isMissing,
        string missingBackground,
        string filledBackground,
        string borderColor,
        string missingTextBrushKey,
        string? filledTextBrushKey)
    {
        chip.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isMissing ? missingBackground : filledBackground));
        chip.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(borderColor));
        text.Foreground = isMissing
            ? (Brush)FindResource(missingTextBrushKey)
            : filledTextBrushKey == null
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB9857B"))
                : (Brush)FindResource(filledTextBrushKey);
        text.FontWeight = isMissing ? FontWeights.Normal : FontWeights.Medium;
    }

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
        var anim = (Storyboard)Resources["HoverEnterAnim"];
        anim?.Begin();
    }

    private void Card_MouseLeave(object sender, MouseEventArgs e)
    {
        var anim = (Storyboard)Resources["HoverLeaveAnim"];
        anim?.Begin();
        HideMoreMenu();
    }

    private void Card_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _mouseDownPos = e.GetPosition(this);
        var anim = (Storyboard)Resources["PressAnim"];
        anim?.Begin();
    }

    private void Card_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isMenuOpen)
        {
            e.Handled = true;
            return;
        }
        var pos = e.GetPosition(this);
        if (Math.Abs(pos.X - _mouseDownPos.X) < 5 && Math.Abs(pos.Y - _mouseDownPos.Y) < 5)
        {
            RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
        }
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (_isMenuOpen) return;
        RaiseEvent(new RoutedEventArgs(CardClickedEvent, this));
    }

    private void Favorite_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not global::ClosetApp.Domain.Entities.Clothing) return;

        var heart = FavoriteBtn.Template.FindName("HeartIcon", FavoriteBtn) as FrameworkElement;
        if (heart == null) return;

        var expand = new DoubleAnimation(20, TimeSpan.FromMilliseconds(120))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        var shrink = new DoubleAnimation(14, TimeSpan.FromMilliseconds(180))
        { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

        expand.Completed += (s, args) =>
        {
            heart.BeginAnimation(WidthProperty, shrink);
            heart.BeginAnimation(HeightProperty, shrink);
        };

        heart.BeginAnimation(WidthProperty, expand);
        heart.BeginAnimation(HeightProperty, expand);

        e.Handled = true;
    }

    private void More_Click(object sender, RoutedEventArgs e)
    {
        if (_isMenuOpen)
            HideMoreMenu();
        else
            ShowMoreMenu();
        e.Handled = true;
    }

    private void ShowMoreMenu()
    {
        MoreMenu.Visibility = Visibility.Visible;
        _isMenuOpen = true;
    }

    private void HideMoreMenu()
    {
        MoreMenu.Visibility = Visibility.Collapsed;
        _isMenuOpen = false;
    }

    private void MenuEdit_Click(object sender, RoutedEventArgs e)
    {
        HideMoreMenu();
        RaiseEvent(new RoutedEventArgs(EditClickedEvent, this));
        e.Handled = true;
    }

    private void MenuDelete_Click(object sender, RoutedEventArgs e)
    {
        HideMoreMenu();
        RaiseEvent(new RoutedEventArgs(DeleteClickedEvent, this));
        e.Handled = true;
    }
}
