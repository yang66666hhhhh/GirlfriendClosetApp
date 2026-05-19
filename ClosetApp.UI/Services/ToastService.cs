using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClosetApp.UI.Services;

public class ToastService
{
    private const string ToastOverlayTag = "__ClosetAppToastOverlay";
    private static readonly TimeSpan SuccessDuration = TimeSpan.FromSeconds(2.2);
    private static readonly TimeSpan InfoDuration = TimeSpan.FromSeconds(3.0);
    private static readonly TimeSpan ErrorDuration = TimeSpan.FromSeconds(4.8);

    private static readonly Lazy<ToastService> _instance = new(() => new ToastService());
    public static ToastService Instance => _instance.Value;

    private readonly List<Border> _activeToasts = new();

    public void ShowSuccess(string message, string? detail = null)
        => ShowToast(message, detail, new SolidColorBrush(Color.FromRgb(16, 185, 129)), SuccessDuration);

    public void ShowError(string message, string? detail = null)
        => ShowToast(message, detail, new SolidColorBrush(Color.FromRgb(239, 68, 68)), ErrorDuration);

    public void ShowInfo(string message, string? detail = null)
        => ShowToast(message, detail, new SolidColorBrush(Color.FromRgb(102, 126, 234)), InfoDuration);

    private void ShowToast(string message, string? detail, Brush backgroundBrush, TimeSpan displayDuration)
    {
        try
        {
            if (System.Windows.Application.Current?.MainWindow == null) return;

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var toast = new Border
                {
                    Background = backgroundBrush,
                    CornerRadius = new CornerRadius(12),
                    Padding = new Thickness(20, 12, 20, 12),
                    Margin = new Thickness(0, 0, 0, 10),
                    MaxWidth = 360,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 20,
                        ShadowDepth = 4,
                        Opacity = 0.2,
                        Color = Colors.Black
                    }
                };

                var content = new StackPanel();

                var titleText = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium,
                    TextWrapping = TextWrapping.Wrap
                };
                content.Children.Add(titleText);

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

                toast.Child = content;

                var mainWindow = System.Windows.Application.Current.MainWindow;
                var grid = mainWindow.Content as Grid;
                if (grid == null) return;

                var toastHost = GetOrCreateToastHost(grid);
                toastHost.Children.Add(toast);
                _activeToasts.Add(toast);

                toast.RenderTransform = new TranslateTransform(300, 0);
                var slideIn = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);

                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
                {
                    BeginTime = displayDuration,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                void OnCompleted(object? s, EventArgs e)
                {
                    try
                    {
                        toastHost.Children.Remove(toast);
                        _activeToasts.Remove(toast);
                    }
                    catch { }
                }

                fadeOut.Completed += OnCompleted;
                toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            });
        }
        catch { }
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
            IsHitTestVisible = false
        };

        var toastOverlay = new Grid
        {
            Tag = ToastOverlayTag,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            IsHitTestVisible = false
        };
        Panel.SetZIndex(toastOverlay, 3000);
        Grid.SetColumnSpan(toastOverlay, Math.Max(1, root.ColumnDefinitions.Count));
        Grid.SetRowSpan(toastOverlay, Math.Max(1, root.RowDefinitions.Count));
        toastOverlay.Children.Add(host);
        root.Children.Add(toastOverlay);

        return host;
    }
}
