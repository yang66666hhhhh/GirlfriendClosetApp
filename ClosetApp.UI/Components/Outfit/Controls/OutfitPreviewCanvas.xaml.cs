using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using ClosetApp.Application.Images;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Components.Outfit.Engine;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Outfit.Controls;

using ClothingEntity = global::ClosetApp.Domain.Entities.Clothing;

public partial class OutfitPreviewCanvas : UserControl
{
    private readonly OutfitCompositionEngine _engine;
    private bool _pendingRender;
    private DispatcherOperation? _scheduledRender;

    public static readonly DependencyProperty ClothesProperty =
        DependencyProperty.Register(
            nameof(Clothes),
            typeof(IList<global::ClosetApp.Domain.Entities.Clothing>),
            typeof(OutfitPreviewCanvas),
            new PropertyMetadata(null, OnClothesChanged));

    public IList<global::ClosetApp.Domain.Entities.Clothing>? Clothes
    {
        get => (IList<global::ClosetApp.Domain.Entities.Clothing>?)GetValue(ClothesProperty);
        set => SetValue(ClothesProperty, value);
    }

    public OutfitPreviewCanvas()
    {
        InitializeComponent();
        _engine = new OutfitCompositionEngine();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        IsVisibleChanged += (_, e) =>
        {
            if ((bool)e.NewValue && _pendingRender)
                Render();
        };
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ScheduleRender();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeInfo)
    {
        if (sizeInfo.NewSize.Width > 0 && sizeInfo.NewSize.Height > 0)
            ScheduleRender();
    }

    private static void OnClothesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OutfitPreviewCanvas canvas)
            canvas.ScheduleRender();
    }

    private double MeasureCanvasWidth()
    {
        if (ActualWidth > 10) return ActualWidth;
        if (Parent is FrameworkElement parent && parent.ActualWidth > 10)
            return Math.Min(parent.ActualWidth - 24, 350);
        if (Stage?.ActualWidth > 10) return Math.Min(Stage.ActualWidth - 24, 350);
        return 280;
    }

    private double MeasureCanvasHeight()
    {
        if (ActualHeight > 10) return ActualHeight;
        if (Parent is FrameworkElement parent && parent.ActualHeight > 10)
            return Math.Min(parent.ActualHeight - 24, 480);
        if (Stage?.ActualHeight > 10) return Math.Min(Stage.ActualHeight - 24, 480);
        return 360;
    }

    public void Render()
    {
        if (!IsVisible)
        {
            _pendingRender = true;
            return;
        }
        _pendingRender = false;

        RenderCanvas.Children.Clear();

        if (Clothes == null || Clothes.Count == 0) return;

        double cw = MeasureCanvasWidth();
        double ch = MeasureCanvasHeight();

        if (cw <= 0 || ch <= 0) return;

        RenderCanvas.Width = cw;
        RenderCanvas.Height = ch;

        var layout = _engine.CalculateLayout(Clothes, cw, ch);
        var layoutItems = layout.Items;

        foreach (var item in layoutItems)
        {
            var shadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = item.IsInset ? 5 : 7,
                ShadowDepth = item.IsInset ? 1 : 2,
                Opacity = item.IsInset ? 0.035 : 0.055,
                Direction = 270
            };

            var imageSource = ClothingImageLoader.Load(
                item.Clothing.ImagePath,
                ImageVariant.Display,
                (int)Math.Clamp(Math.Ceiling(item.Width * 1.4), 160, 360),
                trimLightPadding: true,
                extractForeground: true);

            if (imageSource == null)
            {
                AddMissingImagePlaceholder(item, shadow);
                continue;
            }

            var img = new Image
            {
                Width = item.Width,
                Height = item.Height,
                Stretch = Stretch.Uniform,
                Effect = shadow,
                Opacity = item.Opacity,
                RenderTransform = Transform.Identity,
                Source = imageSource
            };

            Canvas.SetLeft(img, item.X);
            Canvas.SetTop(img, item.Y);
            Canvas.SetZIndex(img, item.ZIndex);
            RenderCanvas.Children.Add(img);
        }

#if DEBUG
        Debug.WriteLine($"[Canvas] Render: Items={layoutItems.Count}, Canvas={cw:F0}x{ch:F0}, Children={RenderCanvas.Children.Count}");
#endif
    }

    private void ScheduleRender()
    {
        if (!IsLoaded)
        {
            _pendingRender = true;
            return;
        }

        if (_scheduledRender is { Status: DispatcherOperationStatus.Pending or DispatcherOperationStatus.Executing })
            return;

        _scheduledRender = Dispatcher.BeginInvoke(Render, DispatcherPriority.Render);
    }

    private void AddMissingImagePlaceholder(OutfitLayoutItem item, DropShadowEffect shadow)
    {
        var border = new Border
        {
            Width = item.Width,
            Height = item.Height,
            CornerRadius = new CornerRadius(Math.Min(18, Math.Max(10, item.Width * 0.09))),
            Background = ResolveBrush("SurfaceCardBrush", new SolidColorBrush(Color.FromRgb(255, 255, 255))),
            BorderBrush = ResolveBrush("BorderLightBrush", new SolidColorBrush(Color.FromRgb(230, 224, 221))),
            BorderThickness = new Thickness(1),
            Effect = shadow,
            Opacity = item.Opacity,
            Padding = new Thickness(8)
        };

        var panel = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center
        };

        var title = new TextBlock
        {
            Text = ResolveClothingName(item.Clothing),
            Foreground = ResolveBrush("TextPrimaryBrush", new SolidColorBrush(Color.FromRgb(55, 50, 49))),
            FontWeight = FontWeights.SemiBold,
            FontSize = Math.Clamp(item.Width / 10, 10, 13),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = Math.Max(24, item.Height * 0.42)
        };

        var detail = new TextBlock
        {
            Text = $"{ResolveClothingTypeName(item.Clothing)} / 图片缺失",
            Foreground = ResolveBrush("TextSecondaryBrush", new SolidColorBrush(Color.FromRgb(125, 116, 112))),
            FontSize = Math.Clamp(item.Width / 12, 9, 11),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 5, 0, 0)
        };

        if (!string.IsNullOrWhiteSpace(item.Clothing.Color))
        {
            detail.Text = $"{detail.Text}\n{item.Clothing.Color}";
        }

        panel.Children.Add(title);
        panel.Children.Add(detail);
        border.Child = panel;

        Canvas.SetLeft(border, item.X);
        Canvas.SetTop(border, item.Y);
        Canvas.SetZIndex(border, item.ZIndex);
        RenderCanvas.Children.Add(border);
    }

    private static string ResolveClothingName(ClothingEntity clothing)
    {
        var name = clothing.Name?.Trim();
        return string.IsNullOrWhiteSpace(name) ? "历史单品" : name;
    }

    private static string ResolveClothingTypeName(ClothingEntity clothing)
    {
        if (clothing.GarmentType.HasValue)
            return ClothingMappings.GetDisplayName(clothing.GarmentType.Value);

        return clothing.Type switch
        {
            ClothingType.Top => "上衣",
            ClothingType.Bottom => "下装",
            ClothingType.Outerwear => "外套",
            ClothingType.Dress => "连衣裙",
            ClothingType.Skirt => "半裙",
            ClothingType.Shoes => "鞋子",
            ClothingType.Accessory => "配饰",
            _ => "单品"
        };
    }

    private static Brush ResolveBrush(string resourceKey, Brush fallback)
    {
        return System.Windows.Application.Current?.TryFindResource(resourceKey) as Brush ?? fallback;
    }
}
