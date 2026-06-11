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
        var surfacePage = GetThemeColor("SurfacePageBrush", Color.FromRgb(249, 245, 241));

        CardBorder.BorderThickness = new Thickness(IsSelected ? 2 : 1);
        CardBorder.BorderBrush = new SolidColorBrush(IsSelected ? primary : borderLight);
        CardBorder.Background = new SolidColorBrush(SurfaceColor);
        CardBorder.Effect = IsSelected
            ? new DropShadowEffect { Color = primary, BlurRadius = 18, ShadowDepth = 0, Opacity = 0.14 }
            : null;
        CardRootButton.IsEnabled = !IsSelected;
        CardRootButton.Cursor = IsSelected ? Cursors.Arrow : Cursors.Hand;

        SelectedBadge.Visibility = IsSelected ? Visibility.Visible : Visibility.Collapsed;
        SelectedBadge.Background = new SolidColorBrush(primaryLight);
        SelectedBadge.BorderBrush = new SolidColorBrush(primary);
        if (SelectedBadge.Child is TextBlock badgeText)
            badgeText.Foreground = new SolidColorBrush(primary);
        ActionPill.BorderBrush = new SolidColorBrush(IsSelected ? primary : borderLight);
        ActionPill.Background = new SolidColorBrush(IsSelected ? primaryLight : Colors.White);
        ActionPillText.Text = IsSelected ? "已启用" : "点按切换";
        ActionPillText.Foreground = new SolidColorBrush(IsSelected ? primary : textSecondary);

        TxtName.Text = DisplayName;
        TxtName.Foreground = new SolidColorBrush(textPrimary);
        TxtDescription.Text = Description;
        TxtDescription.Foreground = new SolidColorBrush(textSecondary);
        StateHint.Text = IsSelected ? "当前正在使用" : "点击切换到这套主题";
        StateHint.Foreground = new SolidColorBrush(IsSelected ? primary : textSecondary);
        ActionHint.Text = IsSelected ? "已启用" : $"使用{DisplayName}";
        ActionHint.Foreground = new SolidColorBrush(IsSelected ? textSecondary : primary);

        SwatchPrimary.Background = new SolidColorBrush(PrimaryColor);
        SwatchSoft.Background = new SolidColorBrush(SoftColor);
        SwatchSurface.Background = new SolidColorBrush(SurfaceColor);
        PreviewBorder.Background = new SolidColorBrush(surfacePage);
        PreviewBorder.BorderBrush = new SolidColorBrush(IsSelected ? primaryLight : borderLight);
        PreviewBorder.BorderThickness = new Thickness(1);
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
