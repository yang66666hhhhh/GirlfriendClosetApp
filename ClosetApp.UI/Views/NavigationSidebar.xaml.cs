using System.Threading;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Components.Shared.Editor;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class NavigationSidebar : UserControl
{
    private const double ExpandedSidebarWidth = 180;
    private const double CollapsedSidebarWidth = 88;
    public event EventHandler<int>? NavigationChanged;
    public event EventHandler<bool>? CollapseStateChanged;
    public event EventHandler? PersonalCenterRequested;

    private bool _isCollapsed;
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAiAssetStorageService _assetStorageService;
    private readonly SemaphoreSlim _refreshUserGate = new(1, 1);
    private LocalUser? _currentUser;
    private bool _profileDialogPrewarmed;
    private bool _userManagementDialogPrewarmed;

    public bool IsCollapsed => _isCollapsed;

    public NavigationSidebar()
    {
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        _currentUserContext = App.Services.GetRequiredService<ICurrentUserContext>();
        _assetStorageService = App.Services.GetRequiredService<IAiAssetStorageService>();
        InitializeComponent();
        Loaded += NavigationSidebar_Loaded;
        _currentUserContext.CurrentUserChanged += CurrentUserContext_CurrentUserChanged;
    }

    public async Task RefreshCurrentUserAsync()
    {
        await _refreshUserGate.WaitAsync();
        try
        {
            _currentUser = await _localUserService.GetCurrentAsync();
            TxtCurrentUserName.Text = _currentUser.DisplayName;
            CurrentUserAvatar.AvatarPath = ResolveAvatarPath(_currentUser);
            CurrentUserAvatar.Initial = BuildAvatarInitial(_currentUser.DisplayName);
            CurrentUserAvatar.IsCurrent = true;
            CollapsedCurrentUserAvatar.AvatarPath = CurrentUserAvatar.AvatarPath;
            CollapsedCurrentUserAvatar.Initial = CurrentUserAvatar.Initial;
            CollapsedCurrentUserAvatar.IsCurrent = true;
            RebuildUserMenu();
            ScheduleModalPrewarm();
        }
        finally
        {
            _refreshUserGate.Release();
        }
    }

    public void SetClothingCount(int count)
    {
        TxtClothingCount.Text = $"{count}件衣服";
    }

    public void SetSelectedTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                NavWardrobe.IsChecked = true;
                break;
            case 1:
                NavOutfits.IsChecked = true;
                break;
            case 2:
                NavTags.IsChecked = true;
                break;
            case 3:
                NavSettings.IsChecked = true;
                break;
        }
    }

    private void NavItem_Checked(object sender, RoutedEventArgs e)
    {
        if (sender == NavWardrobe)
            NavigationChanged?.Invoke(this, 0);
        else if (sender == NavOutfits)
            NavigationChanged?.Invoke(this, 1);
        else if (sender == NavTags)
            NavigationChanged?.Invoke(this, 2);
        else if (sender == NavSettings)
            NavigationChanged?.Invoke(this, 3);
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        ToggleCollapse();
    }

    public void ToggleCollapse()
    {
        _isCollapsed = !_isCollapsed;
        CollapseStateChanged?.Invoke(this, _isCollapsed);

        var widthAnim = new DoubleAnimation(_isCollapsed ? CollapsedSidebarWidth : ExpandedSidebarWidth, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(WidthProperty, widthAnim);
        UpdateCollapsedDockState();
    }

    public void Expand()
    {
        if (_isCollapsed)
            ToggleCollapse();
    }

    public void Collapse()
    {
        if (!_isCollapsed)
            ToggleCollapse();
    }

    private async void NavigationSidebar_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateCollapsedDockState();
        await RefreshCurrentUserAsync();
    }

    private async void CurrentUserContext_CurrentUserChanged(object? sender, CurrentUserChangedEventArgs e)
    {
        await Dispatcher.InvokeAsync(async () => await RefreshCurrentUserAsync());
    }

    private void RebuildUserMenu()
    {
        while (ProfileMenu.Items.Count > 0)
            ProfileMenu.Items.RemoveAt(0);

        if (_currentUser != null)
        {
            ProfileMenu.Items.Add(BuildCurrentUserMenuHeader(_currentUser));
            ProfileMenu.Items.Add(BuildProfileMenuDivider());
        }

        ProfileMenu.Items.Add(new MenuItem
        {
            Header = "个人中心",
            Style = (Style)FindResource("WardrobeCard.MoreMenuItem")
        });
        ((MenuItem)ProfileMenu.Items[^1]).Click += OpenPersonalCenter_Click;

        if (_currentUser?.Role == LocalUserRole.SuperAdmin)
        {
            ProfileMenu.Items.Add(new MenuItem
            {
                Header = "用户管理",
                Style = (Style)FindResource("WardrobeCard.MoreMenuItem")
            });
            ((MenuItem)ProfileMenu.Items[^1]).Click += ManageUsers_Click;
        }

        ProfileMenu.Items.Add(BuildProfileMenuDivider());
        ProfileMenu.Items.Add(new MenuItem
        {
            Header = "退出登录",
            Style = (Style)FindResource("WardrobeCard.MoreMenuItem")
        });
        ((MenuItem)ProfileMenu.Items[^1]).Click += Logout_Click;

        if (CollapsedProfileButton.ContextMenu != null)
            CollapsedProfileButton.ContextMenu = ProfileMenu;
    }

    private void Profile_Click(object sender, RoutedEventArgs e)
    {
        if (ProfileMenu == null)
            return;

        var triggerButton = sender as FrameworkElement ?? BtnProfile;
        ProfileMenu.PlacementTarget = triggerButton;
        ProfileMenu.IsOpen = true;
    }

    private void OpenPersonalCenter_Click(object sender, RoutedEventArgs e)
    {
        PersonalCenterRequested?.Invoke(this, EventArgs.Empty);
        ModalService.Instance.ShowCached<PersonalCenterDialog>();
    }

    private void ManageUsers_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != LocalUserRole.SuperAdmin)
        {
            ToastService.Instance.ShowError("无权管理用户", "当前账号无法打开用户管理。");
            return;
        }

        ModalService.Instance.ShowCached<LocalUserManagementDialog>();
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        await _localAuthService.LogoutAsync();
        if (global::System.Windows.Application.Current is App app)
            app.ShowLoginWindow();
        Window.GetWindow(this)?.Close();
    }

    private FrameworkElement BuildCurrentUserMenuHeader(LocalUser user)
    {
        var avatar = new LocalUserAvatar
        {
            Width = 52,
            Height = 52,
            AvatarPath = ResolveAvatarPath(user),
            Initial = BuildAvatarInitial(user.DisplayName),
            IsCurrent = true
        };

        var name = new TextBlock
        {
            Text = user.DisplayName,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        name.SetResourceReference(TextBlock.ForegroundProperty, "TextPrimaryBrush");

        var account = new TextBlock
        {
            Text = $"@{user.AccountName}",
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        account.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");

        var text = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 150
        };
        text.Children.Add(name);
        text.Children.Add(account);

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(text, 1);
        grid.Children.Add(avatar);
        grid.Children.Add(text);

        var shell = new Border
        {
            Margin = new Thickness(2, 2, 2, 6),
            Padding = new Thickness(10),
            CornerRadius = new CornerRadius(12),
            Child = grid,
            IsHitTestVisible = false,
            Focusable = false
        };
        shell.MinWidth = 192;
        shell.SetResourceReference(Border.BackgroundProperty, "SurfaceSectionBrush");
        shell.SetResourceReference(Border.BorderBrushProperty, "BorderLightBrush");
        shell.BorderThickness = new Thickness(1);
        return new MenuItem
        {
            Header = shell,
            IsEnabled = false,
            Style = (Style)FindResource("ProfileMenuHeaderItemStyle")
        };
    }

    private MenuItem BuildProfileMenuDivider()
    {
        var divider = new Border
        {
            Height = 1,
            Margin = new Thickness(10, 6, 10, 6),
            Opacity = 0.9,
            SnapsToDevicePixels = true
        };

        divider.SetResourceReference(Border.BackgroundProperty, "BorderLightBrush");

        return new MenuItem
        {
            Header = divider,
            IsEnabled = false,
            Style = (Style)FindResource("ProfileMenuDividerStyle")
        };
    }

    private static string BuildAvatarInitial(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? "衣"
            : displayName.Trim()[0].ToString();
    }

    private string? ResolveAvatarPath(LocalUser user)
    {
        if (string.IsNullOrWhiteSpace(user.AvatarPhotoPath))
            return null;

        return Path.IsPathRooted(user.AvatarPhotoPath)
            ? user.AvatarPhotoPath
            : _assetStorageService.GetProfileReferenceFullPath(user.AvatarPhotoPath, user.Id);
    }

    private void ScheduleModalPrewarm()
    {
        if (!_profileDialogPrewarmed)
        {
            _profileDialogPrewarmed = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() => ModalService.Instance.PrewarmCached<PersonalCenterDialog>()));
        }

        if (_currentUser?.Role == LocalUserRole.SuperAdmin && !_userManagementDialogPrewarmed)
        {
            _userManagementDialogPrewarmed = true;
            Dispatcher.BeginInvoke(
                DispatcherPriority.ApplicationIdle,
                new Action(() => ModalService.Instance.PrewarmCached<LocalUserManagementDialog>()));
        }
    }

    private void UpdateCollapsedDockState()
    {
        HeaderPanel.Margin = _isCollapsed
            ? new Thickness(0, 18, 0, 18)
            : new Thickness(16, 18, 16, 20);
        Width = _isCollapsed ? CollapsedSidebarWidth : ExpandedSidebarWidth;

        BtnProfile.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        CollapsedProfileButton.Visibility = _isCollapsed ? Visibility.Visible : Visibility.Collapsed;
        ProfileTextPanel.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarWorkspaceTitle.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;

        BtnProfile.Padding = new Thickness(0);
        BtnProfile.HorizontalContentAlignment = HorizontalAlignment.Stretch;

        SidebarNavHost.Margin = _isCollapsed
            ? new Thickness(0, 8, 0, 0)
            : new Thickness(0, 10, 0, 0);
        SidebarDivider.Width = _isCollapsed ? 28 : 40;
        SidebarDivider.Margin = _isCollapsed
            ? new Thickness(0, 18, 0, 16)
            : new Thickness(0, 16, 0, 14);

        if (_isCollapsed)
            ApplyNavDockMode();
        else
            ApplyNavExpandedMode();
        UpdateNavTooltips();

        SidebarCollapseButton.ToolTip = _isCollapsed ? "展开侧边栏" : "收起侧边栏";
        CollapseGlyph.Text = _isCollapsed ? "▶" : "◀";
        SidebarCollapseButton.HorizontalAlignment = _isCollapsed ? HorizontalAlignment.Center : HorizontalAlignment.Right;
        SidebarCollapseButton.Margin = _isCollapsed
            ? new Thickness(0, 0, 0, 0)
            : new Thickness(0, 0, 0, 0);
    }

    private void ApplyNavDockMode()
    {
        ApplyDockStyle(NavWardrobe);
        ApplyDockStyle(NavOutfits);
        ApplyDockStyle(NavTags);
        ApplyDockStyle(NavSettings);
    }

    private void ApplyNavExpandedMode()
    {
        // 展开态恢复为完整清单宽度，避免沿用图标坞尺寸导致文本区被压缩截断。
        ApplyExpandedStyle(NavWardrobe);
        ApplyExpandedStyle(NavOutfits);
        ApplyExpandedStyle(NavTags);
        ApplyExpandedStyle(NavSettings);
    }

    private static void ApplyDockStyle(RadioButton button)
    {
        button.Width = 48;
        button.Height = 44;
        button.HorizontalAlignment = HorizontalAlignment.Center;
        button.Margin = new Thickness(0, 0, 0, 8);
        button.Padding = new Thickness(0);
    }

    private static void ApplyExpandedStyle(RadioButton button)
    {
        button.Width = double.NaN;
        button.Height = 44;
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.Margin = new Thickness(12, 0, 12, 0);
        button.Padding = new Thickness(0);
    }

    private void UpdateNavTooltips()
    {
        NavWardrobe.ToolTip = "衣柜";
        NavOutfits.ToolTip = "搭配";
        NavTags.ToolTip = "标签";
        NavSettings.ToolTip = "设置";
    }
}
