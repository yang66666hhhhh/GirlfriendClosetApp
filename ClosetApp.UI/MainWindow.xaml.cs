using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Views;
using Serilog;

namespace ClosetApp.UI;

public partial class MainWindow : Window
{
    private int _currentTabIndex = 0;

    public MainWindow()
    {
        InitializeComponent();
        SizeChanged += MainWindow_SizeChanged;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveSidebar();
    }

    private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        ApplyResponsiveSidebar();
    }

    private void ApplyResponsiveSidebar()
    {
        if (ActualWidth < 1000 && !Sidebar.IsCollapsed)
            Sidebar.Collapse();
        else if (ActualWidth >= 1200 && Sidebar.IsCollapsed)
            Sidebar.Expand();
    }

    private void Sidebar_NavigationChanged(object? sender, int tabIndex)
    {
        ShowTab(tabIndex);

        if (tabIndex == 1)
        {
            Log.Debug("Refreshing outfits after navigating to outfits tab");
            _ = OutfitsTabContent.RefreshAsync();
        }
    }

    private void Sidebar_CollapseStateChanged(object? sender, bool isCollapsed)
    {
        var targetWidth = isCollapsed ? 72.0 : 220.0;
        var anim = new GridLengthAnimation
        {
            From = new GridLength(SidebarColumn.Width.Value),
            To = new GridLength(targetWidth),
            Duration = TimeSpan.FromMilliseconds(220),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        SidebarColumn.BeginAnimation(ColumnDefinition.WidthProperty, anim);
    }

    public void NavigateToSettings()
    {
        Sidebar.SetSelectedTab(3);
        ShowTab(3);
    }

    private void ShowTab(int tabIndex)
    {
        _currentTabIndex = tabIndex;
        ClothesTabContent.Visibility = tabIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        OutfitsTabContent.Visibility = tabIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        TagsTabContent.Visibility = tabIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        SettingsTabContent.Visibility = tabIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
    }
}
