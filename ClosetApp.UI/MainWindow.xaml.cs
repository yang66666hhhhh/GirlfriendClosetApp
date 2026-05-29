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
        ClothesTabContent.ClothingCountChanged += ClothesTabContent_ClothingCountChanged;
        SizeChanged += MainWindow_SizeChanged;
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveSidebar();
    }

    private void ClothesTabContent_ClothingCountChanged(object? sender, int count)
    {
        Sidebar.SetClothingCount(count);
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

    private async void Sidebar_NavigationChanged(object? sender, int tabIndex)
    {
        ShowTab(tabIndex);
        await RefreshVisibleTabAsync(tabIndex);
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

    public async Task NavigateToSettingsAsync()
    {
        Sidebar.SetSelectedTab(3);
        ShowTab(3);
        await RefreshVisibleTabAsync(3);
    }

    public async Task RefreshDataTabsAsync(
        bool clothes = false,
        bool outfits = false,
        bool tags = false,
        bool settings = false)
    {
        if (clothes)
            await ClothesTabContent.RefreshAsync();

        if (outfits)
            await OutfitsTabContent.RefreshAsync();

        if (tags)
            await TagsTabContent.RefreshAsync();

        if (settings)
            await SettingsTabContent.RefreshAsync();
    }

    private void ShowTab(int tabIndex)
    {
        _currentTabIndex = tabIndex;
        ClothesTabContent.Visibility = tabIndex == 0 ? Visibility.Visible : Visibility.Collapsed;
        OutfitsTabContent.Visibility = tabIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        TagsTabContent.Visibility = tabIndex == 2 ? Visibility.Visible : Visibility.Collapsed;
        SettingsTabContent.Visibility = tabIndex == 3 ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task RefreshVisibleTabAsync(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                await ClothesTabContent.RefreshAsync();
                break;
            case 1:
                Log.Debug("Refreshing outfits after navigating to outfits tab");
                await OutfitsTabContent.RefreshAsync();
                break;
            case 2:
                await TagsTabContent.RefreshAsync();
                break;
            case 3:
                await SettingsTabContent.RefreshAsync();
                break;
        }
    }
}
