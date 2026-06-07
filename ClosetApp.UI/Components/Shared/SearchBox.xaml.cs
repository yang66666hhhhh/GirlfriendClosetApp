using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClosetApp.UI.Components.Shared;

public partial class SearchBox : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SearchBox), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty BoxHeightProperty =
        DependencyProperty.Register(nameof(BoxHeight), typeof(double), typeof(SearchBox), new PropertyMetadata(40d));

    public SearchBox()
    {
        InitializeComponent();

        Loaded += (_, _) => UpdateVisualState();
        InputBox.GotKeyboardFocus += (_, _) => UpdateVisualState();
        InputBox.LostKeyboardFocus += (_, _) => UpdateVisualState();
        InputBox.MouseEnter += (_, _) => UpdateVisualState();
        InputBox.MouseLeave += (_, _) => UpdateVisualState();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public double BoxHeight
    {
        get => (double)GetValue(BoxHeightProperty);
        set => SetValue(BoxHeightProperty, value);
    }

    private void UpdateVisualState()
    {
        if (!IsLoaded)
        {
            return;
        }

        var borderBrushKey = InputBox.IsKeyboardFocusWithin ? "PrimaryBrush" : "BorderLightBrush";
        var backgroundKey = InputBox.IsKeyboardFocusWithin || InputBox.IsMouseOver ? "SurfaceCardBrush" : "SurfaceElevatedBrush";

        Shell.BorderBrush = (Brush)FindResource(borderBrushKey);
        Shell.Background = (Brush)FindResource(backgroundKey);
    }
}
