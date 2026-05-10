using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ClosetApp.UI.Services;

public class ToastService
{
    private static readonly Lazy<ToastService> _instance = new(() => new ToastService());
    public static ToastService Instance => _instance.Value;

    private readonly List<Border> _activeToasts = new();
    private readonly object _lock = new();

    public void ShowSuccess(string message) => ShowToast(message, new SolidColorBrush(Color.FromRgb(16, 185, 129)));
    public void ShowError(string message) => ShowToast(message, new SolidColorBrush(Color.FromRgb(239, 68, 68)));
    public void ShowInfo(string message) => ShowToast(message, new SolidColorBrush(Color.FromRgb(102, 126, 234)));

    private void ShowToast(string message, Brush backgroundBrush)
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
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Effect = new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 20,
                        ShadowDepth = 4,
                        Opacity = 0.2,
                        Color = Colors.Black
                    }
                };

                var text = new TextBlock
                {
                    Text = message,
                    Foreground = Brushes.White,
                    FontSize = 14,
                    FontWeight = FontWeights.Medium
                };

                toast.Child = text;

                var mainWindow = System.Windows.Application.Current.MainWindow;
                var grid = mainWindow.Content as Grid;
                if (grid == null) return;

                var toastContainer = new Canvas { Name = "ToastContainer" };
                Grid.SetRow(toastContainer, 0);
                grid.Children.Add(toastContainer);

                Canvas.SetRight(toast, 20);
                Canvas.SetTop(toast, 20 + (_activeToasts.Count * 70));
                toastContainer.Children.Add(toast);
                _activeToasts.Add(toast);

                toast.RenderTransform = new TranslateTransform(300, 0);
                var slideIn = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                toast.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);

                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(300))
                {
                    BeginTime = TimeSpan.FromSeconds(2.5),
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                void OnCompleted(object? s, EventArgs e)
                {
                    try
                    {
                        toastContainer.Children.Remove(toast);
                        _activeToasts.Remove(toast);
                        RepositionToasts();
                    }
                    catch { }
                }

                fadeOut.Completed += OnCompleted;
                toast.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            });
        }
        catch { }
    }

    private void RepositionToasts()
    {
        for (int i = 0; i < _activeToasts.Count; i++)
        {
            Canvas.SetTop(_activeToasts[i], 20 + (i * 70));
        }
    }
}