using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure;
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
        MenuManageUsers.Visibility = _currentUser.Role == LocalUserRole.SuperAdmin ? Visibility.Visible : Visibility.Collapsed;
        ApplyAvatar(_currentUser.AvatarPhotoPath);
        await RebuildUserMenuAsync();
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

    private async Task RebuildUserMenuAsync()
    {
        while (ProfileMenu.Items.Count > 4)
            ProfileMenu.Items.RemoveAt(4);

        ProfileMenu.Items.Add(new Separator());
        foreach (var user in await _localUserService.GetAllAsync())
        {
            var item = new MenuItem
            {
                Header = user.Id == _currentUser?.Id ? $"{user.DisplayName} ✓" : user.DisplayName,
                Tag = user.Id,
                IsEnabled = user.Id != _currentUser?.Id,
                Style = (Style)FindResource("WardrobeCard.MoreMenuItem")
            };
            item.Click += SwitchUser_Click;
            ProfileMenu.Items.Add(item);
        }
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

    private async void SwitchUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: Guid userId })
        {
            await _localAuthService.LogoutAsync();
            if (global::System.Windows.Application.Current is App app)
                app.ShowLoginWindow();
            Window.GetWindow(this)?.Close();
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        await _localAuthService.LogoutAsync();
        if (global::System.Windows.Application.Current is App app)
            app.ShowLoginWindow();
        Window.GetWindow(this)?.Close();
    }

    private void ApplyAvatar(string? avatarPath)
    {
        var fullPath = string.IsNullOrWhiteSpace(avatarPath)
            ? null
            : Path.Combine(AppPaths.AiProfileDir, avatarPath);

        var bitmap = string.IsNullOrWhiteSpace(fullPath) || !File.Exists(fullPath)
            ? LoadAvatarBitmap(new Uri("pack://application:,,,/ClosetApp.UI;component/Assets/Icons/app-avatar.png"))
            : LoadAvatarBitmap(new Uri(fullPath));

        AvatarPhotoFill.Fill = new ImageBrush(bitmap)
        {
            Stretch = Stretch.UniformToFill,
            AlignmentX = AlignmentX.Center,
            AlignmentY = AlignmentY.Center
        };
    }

    private static BitmapImage LoadAvatarBitmap(Uri uri)
    {
        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = uri;
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = 96;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private void UpdateProfileCompactState()
    {
        HeaderPanel.Margin = _isCollapsed
            ? new Thickness(10, 28, 10, 24)
            : new Thickness(20, 28, 20, 24);
        BtnProfile.Padding = _isCollapsed
            ? new Thickness(8)
            : new Thickness(8);
        ProfileTextPanel.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        ProfileChevron.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
        TxtClothingCount.Visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;
    }
}
