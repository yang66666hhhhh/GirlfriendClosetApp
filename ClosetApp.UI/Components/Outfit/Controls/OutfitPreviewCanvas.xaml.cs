using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClosetApp.Application.Images;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Components.Outfit.Engine;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Outfit.Controls;

public partial class OutfitPreviewCanvas : UserControl
{
    private readonly OutfitCompositionEngine _engine;
    private bool _pendingRender;

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
        Render();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs sizeInfo)
    {
        if (sizeInfo.NewSize.Width > 0 && sizeInfo.NewSize.Height > 0)
            Render();
    }

    private static void OnClothesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OutfitPreviewCanvas canvas)
            canvas.Render();
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

            var img = new Image
            {
                Width = item.Width,
                Height = item.Height,
                Stretch = Stretch.Uniform,
                Effect = shadow,
                Opacity = item.Opacity,
                RenderTransform = Transform.Identity,
                Source = ClothingImageLoader.Load(
                    item.Clothing.ImagePath,
                    ImageVariant.Display,
                    (int)Math.Clamp(Math.Ceiling(item.Width * 1.4), 160, 360),
                    trimLightPadding: true,
                    extractForeground: true)
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
}
