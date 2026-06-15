using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Views;
using ClosetApp.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ClosetApp.UI;

public partial class MainWindow : Window
{
    private int _currentTabIndex = 0;
    private bool _hasLoadedInitialTab;

    public MainWindow()
    {
        InitializeComponent();
        ClothesTabContent.ClothingCountChanged += ClothesTabContent_ClothingCountChanged;
        Sidebar.PersonalCenterRequested += Sidebar_PersonalCenterRequested;
        App.Services.GetRequiredService<ICurrentUserContext>().CurrentUserChanged += MainWindow_CurrentUserChanged;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
        SizeChanged += MainWindow_SizeChanged;
        Loaded += MainWindow_Loaded;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyResponsiveSidebar();
        if (_hasLoadedInitialTab)
            return;

        _hasLoadedInitialTab = true;
        _ = RefreshVisibleTabAsync(_currentTabIndex);
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        var authSession = App.Services.GetRequiredService<IAuthSessionContext>();
        if (authSession.IsAuthenticated)
            global::System.Windows.Application.Current.Shutdown();
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
        if (_currentTabIndex == tabIndex && _hasLoadedInitialTab)
            return;

        ShowTab(tabIndex);
        await RefreshVisibleTabAsync(tabIndex);
    }

    private void Sidebar_CollapseStateChanged(object? sender, bool isCollapsed)
    {
        var targetWidth = isCollapsed ? 88.0 : 220.0;
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

    // 当前用户资料更新只需要刷新壳层身份信息和设置入口，不应该触发整套数据页重载。
    public async Task RefreshCurrentUserShellAsync()
    {
        await Sidebar.RefreshCurrentUserAsync();
        await SettingsTabContent.RefreshAsync();
    }

    private void ShowTab(int tabIndex)
    {
        _currentTabIndex = tabIndex;
        Sidebar.SetSelectedTab(tabIndex);
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

    private async void Sidebar_PersonalCenterRequested(object? sender, EventArgs e)
    {
        await SettingsTabContent.RefreshAsync();
    }

    private async void MainWindow_CurrentUserChanged(object? sender, CurrentUserChangedEventArgs e)
    {
        await RefreshDataTabsAsync(clothes: true, outfits: true, tags: true, settings: true);
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
            return;

        var textInput = FindAncestor<TextBoxBase>(Keyboard.FocusedElement as DependencyObject);
        var passwordInput = FindAncestor<PasswordBox>(Keyboard.FocusedElement as DependencyObject);

        if (textInput == null && passwordInput == null)
            return;

        switch (e.Key)
        {
            case Key.V:
                if (textInput != null && !textInput.IsReadOnly)
                {
                    textInput.Paste();
                    e.Handled = true;
                }
                else if (passwordInput != null)
                {
                    passwordInput.Paste();
                    e.Handled = true;
                }
                break;

            case Key.C:
                if (textInput != null)
                {
                    textInput.Copy();
                    e.Handled = true;
                }
                break;

            case Key.X:
                if (textInput != null && !textInput.IsReadOnly)
                {
                    textInput.Cut();
                    e.Handled = true;
                }
                break;

            case Key.A:
                if (textInput != null)
                {
                    textInput.SelectAll();
                    e.Handled = true;
                }
                else if (passwordInput != null)
                {
                    passwordInput.SelectAll();
                    e.Handled = true;
                }
                break;
        }
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current != null)
        {
            if (current is T match)
                return match;

            current = current switch
            {
                Visual visual => VisualTreeHelper.GetParent(visual),
                Visual3D visual3D => VisualTreeHelper.GetParent(visual3D),
                FrameworkContentElement contentElement => contentElement.Parent,
                _ => LogicalTreeHelper.GetParent(current)
            };
        }

        return null;
    }
}
