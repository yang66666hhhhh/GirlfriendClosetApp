using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Shared;

public partial class ThemeCard : UserControl
{
    public static readonly DependencyProperty ThemeKindProperty =
        DependencyProperty.Register(nameof(ThemeKind), typeof(AppThemeKind), typeof(ThemeCard),
            new PropertyMetadata(AppThemeKind.Rose, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(ThemeCard),
            new PropertyMetadata(false, OnVisualPropertyChanged));

    public static readonly DependencyProperty DisplayNameProperty =
        DependencyProperty.Register(nameof(DisplayName), typeof(string), typeof(ThemeCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string), typeof(ThemeCard),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty PrimaryColorProperty =
        DependencyProperty.Register(nameof(PrimaryColor), typeof(Color), typeof(ThemeCard),
            new PropertyMetadata(Colors.Transparent));

    public static readonly DependencyProperty SoftColorProperty =
        DependencyProperty.Register(nameof(SoftColor), typeof(Color), typeof(ThemeCard),
            new PropertyMetadata(Colors.Transparent));

    public static readonly DependencyProperty SurfaceColorProperty =
        DependencyProperty.Register(nameof(SurfaceColor), typeof(Color), typeof(ThemeCard),
            new PropertyMetadata(Colors.Transparent));

    public static readonly DependencyProperty SelectedCommandProperty =
        DependencyProperty.Register(nameof(SelectedCommand), typeof(ICommand), typeof(ThemeCard));

    public static readonly RoutedEvent SelectedEvent =
        EventManager.RegisterRoutedEvent("Selected", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(ThemeCard));

    public AppThemeKind ThemeKind
    {
        get => (AppThemeKind)GetValue(ThemeKindProperty);
        set => SetValue(ThemeKindProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public string DisplayName
    {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public Color PrimaryColor
    {
        get => (Color)GetValue(PrimaryColorProperty);
        set => SetValue(PrimaryColorProperty, value);
    }

    public Color SoftColor
    {
        get => (Color)GetValue(SoftColorProperty);
        set => SetValue(SoftColorProperty, value);
    }

    public Color SurfaceColor
    {
        get => (Color)GetValue(SurfaceColorProperty);
        set => SetValue(SurfaceColorProperty, value);
    }

    public ICommand? SelectedCommand
    {
        get => (ICommand?)GetValue(SelectedCommandProperty);
        set => SetValue(SelectedCommandProperty, value);
    }

    public event RoutedEventHandler Selected
    {
        add => AddHandler(SelectedEvent, value);
        remove => RemoveHandler(SelectedEvent, value);
    }

    public ThemeCard()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyVisualState();
    }

    private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ThemeCard card && card.IsLoaded)
            card.ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        var primary = GetThemeColor("PrimaryBrush", Color.FromRgb(202, 156, 159));
        var primaryLight = GetThemeColor("PrimaryLightBrush", Color.FromRgb(247, 240, 238));
        var borderLight = GetThemeColor("BorderLightBrush", Color.FromRgb(236, 226, 223));
        var textPrimary = GetThemeColor("TextPrimaryBrush", Color.FromRgb(60, 60, 60));
        var textSecondary = GetThemeColor("TextSecondaryBrush", Color.FromRgb(120, 120, 120));

        // Card border
        CardBorder.BorderThickness = new Thickness(IsSelected ? 2.5 : 1.25);
        CardBorder.BorderBrush = new SolidColorBrush(IsSelected ? primary : borderLight);
        CardBorder.Background = new SolidColorBrush(SurfaceColor);
        CardBorder.Effect = IsSelected
            ? new DropShadowEffect { Color = primary, BlurRadius = 20, ShadowDepth = 0, Opacity = 0.18 }
            : null;

        // Badge
        Badge.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
        Badge.Background = new SolidColorBrush(primaryLight);
        Badge.BorderBrush = new SolidColorBrush(primary);
        if (Badge.Child is TextBlock badgeText)
            badgeText.Foreground = new SolidColorBrush(primary);

        // Texts
        TxtName.Text = DisplayName;
        TxtName.Foreground = new SolidColorBrush(textPrimary);
        TxtDescription.Text = Description;
        TxtDescription.Foreground = new SolidColorBrush(textSecondary);

        // Swatches
        SwatchPrimary.Background = new SolidColorBrush(PrimaryColor);
        SwatchSoft.Background = new SolidColorBrush(SoftColor);
        SwatchSurface.Background = new SolidColorBrush(SurfaceColor);

        // Button
        var idleText = $"使用{DisplayName}";
        BtnSelect.Content = IsSelected ? "已启用" : idleText;
        BtnSelect.IsEnabled = !IsSelected;
        BtnSelect.Background = new SolidColorBrush(IsSelected ? SurfaceColor : primaryLight);
        BtnSelect.BorderBrush = new SolidColorBrush(IsSelected ? borderLight : primary);
        BtnSelect.Foreground = new SolidColorBrush(IsSelected ? textSecondary : primary);
        BtnSelect.BorderThickness = new Thickness(1);
    }

    private void BtnSelect_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCommand?.CanExecute(ThemeKind) == true)
            SelectedCommand.Execute(ThemeKind);
        RaiseEvent(new RoutedEventArgs(SelectedEvent, this));
    }

    private static Color GetThemeColor(string key, Color fallback)
    {
        if (global::System.Windows.Application.Current?.TryFindResource(key) is SolidColorBrush brush)
            return brush.Color;
        return fallback;
    }
}
