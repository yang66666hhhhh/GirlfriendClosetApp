using System.Collections.Generic;
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
        RenderCanvas.Children.Clear();

        if (Clothes == null || Clothes.Count == 0) return;

        double cw = MeasureCanvasWidth();
        double ch = MeasureCanvasHeight();

        if (cw <= 0 || ch <= 0) return;

        RenderCanvas.Width = cw;
        RenderCanvas.Height = ch;

        var layoutItems = _engine.CalculateLayout(Clothes, cw, ch);

        foreach (var item in layoutItems)
        {
            var shadow = new DropShadowEffect
            {
                Color = Colors.Black,
                BlurRadius = 10,
                ShadowDepth = 2,
                Opacity = 0.09,
                Direction = 270
            };

            var img = new Image
            {
                Width = item.Width,
                Height = item.Height,
                Stretch = Stretch.Uniform,
                Effect = shadow,
                Opacity = item.Opacity,
                Source = ClothingImageLoader.Load(
                    item.Clothing.ImagePath,
                    ImageVariant.Display,
                    (int)Math.Clamp(Math.Ceiling(item.Width * 1.4), 160, 360))
            };

            if (item.IsInset)
            {
                var insetHost = new Border
                {
                    Width = item.Width + 10,
                    Height = item.Height + 10,
                    Padding = new Thickness(5),
                    CornerRadius = new CornerRadius(12),
                    Background = new SolidColorBrush(Color.FromArgb(232, 255, 253, 252)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(232, 226, 220)),
                    BorderThickness = new Thickness(1),
                    Effect = shadow,
                    Child = img
                };

                img.Effect = null;
                Canvas.SetLeft(insetHost, item.X - 5);
                Canvas.SetTop(insetHost, item.Y - 5);
                Canvas.SetZIndex(insetHost, item.ZIndex);
                RenderCanvas.Children.Add(insetHost);
                continue;
            }

            Canvas.SetLeft(img, item.X);
            Canvas.SetTop(img, item.Y);
            Canvas.SetZIndex(img, item.ZIndex);
            RenderCanvas.Children.Add(img);
        }
    }
}
