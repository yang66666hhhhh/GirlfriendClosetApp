using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    public event EventHandler<int>? NavigationChanged;
    public event EventHandler<bool>? CollapseStateChanged;
    public event EventHandler? PersonalProfileRequested;

    private bool _isCollapsed;
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private readonly ICurrentUserContext _currentUserContext;
    private LocalUser? _currentUser;

    public bool IsCollapsed => _isCollapsed;

    public NavigationSidebar()
    {
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        _currentUserContext = App.Services.GetRequiredService<ICurrentUserContext>();
        InitializeComponent();
        Loaded += NavigationSidebar_Loaded;
        _currentUserContext.CurrentUserChanged += CurrentUserContext_CurrentUserChanged;
    }

    public async Task RefreshCurrentUserAsync()
    {
        _currentUser = await _localUserService.GetCurrentAsync();
        TxtCurrentUserName.Text = _currentUser.DisplayName;
        TxtCurrentUserRole.Text = _currentUser.Role == LocalUserRole.SuperAdmin ? "超级管理员" : "本地用户";
        CurrentUserAvatar.AvatarPath = _currentUser.AvatarPhotoPath;
        CurrentUserAvatar.Initial = BuildAvatarInitial(_currentUser.DisplayName);
        CurrentUserAvatar.IsCurrent = true;
        RebuildUserMenu();
    }

    public void SetClothingCount(int count)
    {
        TxtClothingCount.Text = $"{count} 件衣服";
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

        var rotateTarget = _isCollapsed ? 180.0 : 0.0;
        var rotateAnim = new DoubleAnimation(rotateTarget, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var widthAnim = new DoubleAnimation(_isCollapsed ? 72 : 220, TimeSpan.FromMilliseconds(220))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        BeginAnimation(WidthProperty, widthAnim);

        CollapseRotate.BeginAnimation(RotateTransform.AngleProperty, rotateAnim);
        UpdateProfileCompactState();
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
        UpdateProfileCompactState();
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
            Header = "编辑当前档案",
            Style = (Style)FindResource("WardrobeCard.MoreMenuItem")
        });
        ((MenuItem)ProfileMenu.Items[^1]).Click += EditCurrentProfile_Click;

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
    }

    private void Profile_Click(object sender, RoutedEventArgs e)
    {
        if (BtnProfile.ContextMenu == null)
            return;

        BtnProfile.ContextMenu.PlacementTarget = BtnProfile;
        BtnProfile.ContextMenu.IsOpen = true;
    }

    private void EditCurrentProfile_Click(object sender, RoutedEventArgs e)
    {
        PersonalProfileRequested?.Invoke(this, EventArgs.Empty);
        ModalService.Instance.Show(new PersonalProfileEditorPanel());
    }

    private void ManageUsers_Click(object sender, RoutedEventArgs e)
    {
        if (_currentUser?.Role != LocalUserRole.SuperAdmin)
        {
            ToastService.Instance.ShowError("无权管理用户", "只有超级管理员可以打开用户管理。");
            return;
        }

        ModalService.Instance.Show(new LocalUserManagementDialog());
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
            Width = 48,
            Height = 48,
            AvatarPath = user.AvatarPhotoPath,
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
            Text = $"@{user.AccountName} · {(user.Role == LocalUserRole.SuperAdmin ? "超级管理员" : "本地用户")}",
            Margin = new Thickness(0, 4, 0, 0),
            FontSize = 11,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        account.SetResourceReference(TextBlock.ForegroundProperty, "TextSecondaryBrush");

        var text = new StackPanel
        {
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            MaxWidth = 180
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
            Padding = new Thickness(12),
            CornerRadius = new CornerRadius(14),
            Child = grid,
            IsHitTestVisible = false,
            Focusable = false
        };
        shell.MinWidth = 220;
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

    private FrameworkElement BuildProfileMenuDivider()
    {
        return new Border
        {
            Height = 1,
            Margin = new Thickness(10, 6, 10, 6),
            Background = (Brush)FindResource("BorderLightBrush"),
            Opacity = 0.9,
            IsHitTestVisible = false
        };
    }

    private static string BuildAvatarInitial(string? displayName)
    {
        return string.IsNullOrWhiteSpace(displayName)
            ? "衣"
            : displayName.Trim()[0].ToString();
    }

    private void UpdateProfileCompactState()
    {
        HeaderPanel.Margin = _isCollapsed
            ? new Thickness(10, 28, 10, 24)
            : new Thickness(20, 28, 20, 24);
        BtnProfile.Padding = _isCollapsed
            ? new Thickness(8)
            : new Thickness(10);
        BtnProfile.HorizontalContentAlignment = _isCollapsed
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Stretch;
        ProfileTextPanel.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        ProfileChevron.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        TxtClothingCount.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }
}
