using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace ClosetApp.UI.Services;

public class ToastService
{
    private const string ToastOverlayTag = "__ClosetAppToastOverlay";
    private const int MaxVisibleToasts = 3;
    private static readonly TimeSpan SuccessDuration = TimeSpan.FromSeconds(2.2);
    private static readonly TimeSpan InfoDuration = TimeSpan.FromSeconds(3.0);
    private static readonly TimeSpan ErrorDuration = TimeSpan.FromSeconds(4.8);

    private static readonly Lazy<ToastService> _instance = new(() => new ToastService());
    public static ToastService Instance => _instance.Value;

    private readonly List<Border> _activeToasts = new();

    public void ShowSuccess(string message, string? detail = null)
        => ShowToast(message, detail, "SuccessBrush", SuccessDuration);

    public void ShowError(string message, string? detail = null)
        => ShowToast(message, detail, "ErrorBrush", ErrorDuration);

    public void ShowInfo(string message, string? detail = null)
        => ShowToast(message, detail, "InfoBrush", InfoDuration);

    private void ShowToast(string message, string? detail, string brushKey, TimeSpan displayDuration)
    {
        try
        {
            if (global::System.Windows.Application.Current?.MainWindow == null) return;

            global::System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var backgroundBrush = TryGetBrush(brushKey)
                    ?? new SolidColorBrush(Color.FromRgb(102, 126, 234));

                var toast = BuildToast(message, detail, backgroundBrush, this);
                var mainWindow = global::System.Windows.Application.Current.MainWindow;
                var grid = mainWindow.Content as Grid;
                if (grid == null) return;

                var toastHost = GetOrCreateToastHost(grid);

                // Enforce max visible toasts
                while (_activeToasts.Count >= MaxVisibleToasts)
                {
                    var oldest = _activeToasts[0];
                    _activeToasts.RemoveAt(0);
                    RemoveToastVisual(toastHost, oldest, slideUp: false);
                }

                toastHost.Children.Add(toast);
                _activeToasts.Add(toast);

                // Entrance: slide down from top
                AnimateSlideIn(toast);

                // Auto-dismiss timer
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = displayDuration
                };
                timer.Tick += (_, _) =>
                {
                    timer.Stop();
                    _activeToasts.Remove(toast);
                    RemoveToastVisual(toastHost, toast, slideUp: true);
                };
                toast.Tag = timer;
                timer.Start();
            });
        }
        catch { }
    }

    private static Border BuildToast(string message, string? detail, Brush backgroundBrush, ToastService owner)
    {
        var toast = new Border
        {
            Background = backgroundBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(20, 12, 12, 12),
            Margin = new Thickness(0, 0, 0, 10),
            MaxWidth = 380,
            IsHitTestVisible = true,
            Cursor = Cursors.Arrow,
            Effect = new DropShadowEffect
            {
                BlurRadius = 20,
                ShadowDepth = 4,
                Opacity = 0.2,
                Color = Colors.Black
            }
        };

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Content area
        var content = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(new TextBlock
        {
            Text = message,
            Foreground = Brushes.White,
            FontSize = 14,
            FontWeight = FontWeights.Medium,
            TextWrapping = TextWrapping.Wrap
        });

        if (!string.IsNullOrWhiteSpace(detail))
        {
            content.Children.Add(new TextBlock
            {
                Text = detail,
                Foreground = new SolidColorBrush(Color.FromArgb(224, 255, 255, 255)),
                FontSize = 12,
                Margin = new Thickness(0, 4, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
        }

        Grid.SetColumn(content, 0);
        root.Children.Add(content);

        // Close button
        var closeBtn = new Button
        {
            Content = "\u00D7",
            Width = 28,
            Height = 28,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(0),
            Template = CreateCloseButtonTemplate()
        };

        Grid.SetColumn(closeBtn, 1);
        root.Children.Add(closeBtn);

        toast.Child = root;

        closeBtn.Click += (_, _) =>
        {
            if (toast.Parent is StackPanel host)
            {
                owner._activeToasts.Remove(toast);
                RemoveToastVisual(host, toast, slideUp: true);
            }
        };

        return toast;
    }

    private static ControlTemplate CreateCloseButtonTemplate()
    {
        var template = new ControlTemplate(typeof(Button));
        var border = new FrameworkElementFactory(typeof(Border));
        border.SetValue(Border.NameProperty, "BtnBorder");
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
        border.SetValue(Border.WidthProperty, 28.0);
        border.SetValue(Border.HeightProperty, 28.0);

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(contentPresenter);
        template.VisualTree = border;

        var hoverTrigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty,
            new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)), "BtnBorder"));
        template.Triggers.Add(hoverTrigger);

        return template;
    }

    private static void RemoveToastVisual(StackPanel host, Border toast, bool slideUp)
    {
        // Stop auto-dismiss timer if still running
        if (toast.Tag is System.Windows.Threading.DispatcherTimer timer)
        {
            timer.Stop();
            toast.Tag = null;
        }

        if (toast.Parent != host) return;

        if (slideUp)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };
            var slideOut = new DoubleAnimation(0, -30, TimeSpan.FromMilliseconds(220))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
            };

            void OnCompleted(object? s, EventArgs e)
            {
                fadeOut.Completed -= OnCompleted;
                host.Children.Remove(toast);
            }

            fadeOut.Completed += OnCompleted;
            toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideOut);
        }
        else
        {
            host.Children.Remove(toast);
        }
    }

    private static void AnimateSlideIn(Border toast)
    {
        toast.Opacity = 0;
        toast.RenderTransform = new TranslateTransform(0, -40);

        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        var slideDown = new DoubleAnimation(-40, 0, TimeSpan.FromMilliseconds(280))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        toast.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        toast.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideDown);
    }

    private static StackPanel GetOrCreateToastHost(Grid root)
    {
        foreach (var child in root.Children)
        {
            if (child is Grid { Tag: ToastOverlayTag } existingOverlay &&
                existingOverlay.Children.OfType<StackPanel>().FirstOrDefault() is { } existingHost)
            {
                return existingHost;
            }
        }

        var host = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 24, 24, 0),
            IsHitTestVisible = true
        };

        var toastOverlay = new Grid
        {
            Tag = ToastOverlayTag,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(toastOverlay, 9999);
        Grid.SetColumnSpan(toastOverlay, Math.Max(1, root.ColumnDefinitions.Count));
        Grid.SetRowSpan(toastOverlay, Math.Max(1, root.RowDefinitions.Count));
        toastOverlay.Children.Add(host);
        root.Children.Add(toastOverlay);

        return host;
    }

    private static Brush? TryGetBrush(string key)
    {
        return global::System.Windows.Application.Current?.Resources[key] as Brush;
    }
}
