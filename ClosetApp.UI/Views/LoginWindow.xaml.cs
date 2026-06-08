using System.Windows;
using System.Windows.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Shared;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows.Media;

namespace ClosetApp.UI.Views;

public partial class LoginWindow : Window
{
    private readonly AppStartupCoordinator _startupCoordinator;
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private bool _isSetupMode;
    private bool _isSubmitting;
    private bool _openedMainWindow;
    private LocalUser? _superAdmin;
    private IReadOnlyList<LocalUser> _users = [];
    private readonly List<Button> _recentAccountButtons = [];

    public LoginWindow()
    {
        _startupCoordinator = App.Services.GetRequiredService<AppStartupCoordinator>();
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        InitializeComponent();
        Loaded += LoginWindow_Loaded;
        Closed += LoginWindow_Closed;
        PreviewKeyDown += LoginWindow_PreviewKeyDown;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await _startupCoordinator.WaitUntilReadyAsync();
            await RefreshUsersAsync();
        }
        catch (Exception ex)
        {
            ShowError($"启动初始化失败：{ex.Message}");
        }
    }

    private async Task RefreshUsersAsync()
    {
        _users = await _localUserService.GetAllAsync();
        _superAdmin = _users.FirstOrDefault(user => user.Role == LocalUserRole.SuperAdmin);
        _isSetupMode = _superAdmin != null && !_superAdmin.HasPasswordCredential;

        SetupPanel.Visibility = _isSetupMode ? Visibility.Visible : Visibility.Collapsed;
        LoginPanel.Visibility = _isSetupMode ? Visibility.Collapsed : Visibility.Visible;
        TxtModeTitle.Text = _isSetupMode ? "首次设置管理员密码" : "登录";
        TxtModeDescription.Text = _isSetupMode
            ? "旧数据已归属超级管理员。先设置本机密码，之后每次启动都需要登录。"
            : "输入账号和密码，进入对应衣柜工作区。";
        TxtSubtitle.Text = _isSetupMode
            ? "完成管理员账号密码后，再创建或管理其它本地用户。"
            : "请输入本地账号和密码。登录后如需更换用户，请先退出登录。";
        SetSubmittingState(false);
        if (_isSetupMode && _superAdmin != null)
            SetupAccountBox.Text = string.IsNullOrWhiteSpace(_superAdmin.AccountName)
                ? "admin"
                : _superAdmin.AccountName;
        TxtSelectedUser.Text = "账号";

        if (_isSetupMode)
        {
            SetupAccountBox.Focus();
        }
        else
        {
            ApplyRecentAccountsState();
        }
    }

    private void LoginWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || BtnSubmit.IsEnabled == false)
            return;

        Submit_Click(BtnSubmit, new RoutedEventArgs());
        e.Handled = true;
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            SetSubmittingState(true);
            ClearError();

            if (_isSetupMode)
                await CompleteSetupAsync();
            else
                await LoginAsync();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SetSubmittingState(false);
        }
    }

    private async Task CompleteSetupAsync()
    {
        if (_superAdmin == null)
            throw new InvalidOperationException("未找到超级管理员。");

        if (SetupPasswordBox.Password != SetupConfirmPasswordBox.Password)
            throw new InvalidOperationException("两次输入的密码不一致。");

        if (!string.Equals(_superAdmin.AccountName, SetupAccountBox.Text.Trim(), StringComparison.Ordinal))
            await _localUserService.UpdateAsync(_superAdmin.Id, _superAdmin.DisplayName, accountName: SetupAccountBox.Text);
        await _localAuthService.SetPasswordAsync(_superAdmin.Id, SetupPasswordBox.Password);
        await _localAuthService.SetPinAsync(_superAdmin.Id, SetupPinBox.Password);
        var result = await _localAuthService.LoginAsync(SetupAccountBox.Text, SetupPasswordBox.Password, LocalCredentialKind.Password);
        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? "登录失败。");

        OpenMainWindow();
    }

    private async Task LoginAsync()
    {
        var usePin = !string.IsNullOrWhiteSpace(LoginPinBox.Password);
        var secret = usePin ? LoginPinBox.Password : LoginPasswordBox.Password;
        var kind = usePin ? LocalCredentialKind.Pin : LocalCredentialKind.Password;

        var result = await _localAuthService.LoginAsync(LoginAccountBox.Text, secret, kind);
        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? "登录失败。");

        OpenMainWindow();
    }

    private void ApplyRecentAccountsState()
    {
        var state = LoginRecentAccountsBuilder.Build(_users);
        RecentAccountsPanel.Visibility = state.HasRecentAccounts ? Visibility.Visible : Visibility.Collapsed;
        RecentAccountsHost.Children.Clear();
        _recentAccountButtons.Clear();

        foreach (var account in state.RecentAccounts)
        {
            var button = BuildRecentAccountButton(account);
            _recentAccountButtons.Add(button);
            RecentAccountsHost.Children.Add(button);
        }

        if (!string.IsNullOrWhiteSpace(state.PrefillAccountName))
        {
            LoginAccountBox.Text = state.PrefillAccountName;
            TxtSelectedUser.Text = $"账号 · 上次使用 {state.PrefillAccountName}";
            ApplyRecentAccountSelection(state.PrefillAccountName);
        }
        else
        {
            LoginAccountBox.Clear();
            TxtSelectedUser.Text = "账号";
            ApplyRecentAccountSelection(null);
        }

        LoginPasswordBox.Clear();
        LoginPinBox.Clear();
        FocusCredentialInput();
    }

    private Button BuildRecentAccountButton(LoginRecentAccountItem account)
    {
        var avatar = new LocalUserAvatar
        {
            Width = 40,
            Height = 40,
            Initial = account.Initial,
            IsCurrent = false
        };

        var avatarShell = new Border
        {
            Style = (Style)FindResource("RecentAccountHoverAvatar"),
            Child = avatar
        };

        var name = new TextBlock
        {
            Text = account.DisplayName,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextPrimaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var secondary = new TextBlock
        {
            Text = $"@{account.AccountName}",
            Margin = new Thickness(0, 3, 0, 0),
            FontSize = 11,
            Foreground = (Brush)FindResource("TextSecondaryBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var lastLogin = new TextBlock
        {
            Text = account.LastLoginText,
            Margin = new Thickness(0, 7, 0, 0),
            FontSize = 10,
            Foreground = (Brush)FindResource("TextPlaceholderBrush"),
            TextTrimming = TextTrimming.CharacterEllipsis
        };

        var hintChip = new Border
        {
            Style = (Style)FindResource("RecentAccountHintChip"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 8, 0, 0),
            Child = new TextBlock
            {
                Text = account.HasPinCredential ? "PIN 快捷登录" : "密码登录",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource(account.HasPinCredential ? "PrimaryDarkBrush" : "TextSecondaryBrush")
            }
        };

        var primaryChip = new Border
        {
            Style = (Style)FindResource("RecentAccountPrimaryChip"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(8, 0, 0, 0),
            Visibility = account.IsMostRecent ? Visibility.Visible : Visibility.Collapsed,
            Child = new TextBlock
            {
                Text = "最近使用",
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("PrimaryDarkBrush")
            }
        };

        var text = new StackPanel
        {
            Margin = new Thickness(10, 0, 0, 0),
            Width = 156,
            VerticalAlignment = VerticalAlignment.Center
        };
        text.Children.Add(name);
        text.Children.Add(secondary);
        text.Children.Add(lastLogin);
        text.Children.Add(hintChip);

        var metaRow = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        metaRow.Children.Add(hintChip);
        metaRow.Children.Add(primaryChip);

        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(text, 1);
        row.Children.Add(avatarShell);
        row.Children.Add(text);
        text.Children.Remove(hintChip);
        text.Children.Add(metaRow);

        var button = new Button
        {
            Tag = account,
            Margin = new Thickness(0, 0, 10, 10),
            MinWidth = 0,
            Style = (Style)FindResource("RecentAccountButton"),
            Content = row
        };
        button.Click += RecentAccount_Click;
        return button;
    }

    private void RecentAccount_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: LoginRecentAccountItem account })
            return;

        LoginAccountBox.Text = account.AccountName;
        TxtSelectedUser.Text = account.HasPinCredential
            ? $"账号 · {account.AccountName} 支持 PIN 快速登录"
            : $"账号 · {account.AccountName}";
        ApplyRecentAccountSelection(account.AccountName);
        LoginPasswordBox.Clear();
        LoginPinBox.Clear();
        FocusCredentialInput(account.HasPinCredential);
    }

    private void LoginAccountBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isSetupMode || _isSubmitting)
            return;

        var accountName = LoginAccountBox.Text.Trim();
        ApplyRecentAccountSelection(string.IsNullOrWhiteSpace(accountName) ? null : accountName);
    }

    private void ApplyRecentAccountSelection(string? accountName)
    {
        foreach (var button in _recentAccountButtons)
        {
            var isSelected = button.Tag is LoginRecentAccountItem item &&
                             !string.IsNullOrWhiteSpace(accountName) &&
                             string.Equals(item.AccountName, accountName, StringComparison.OrdinalIgnoreCase);
            button.Style = (Style)FindResource(isSelected ? "RecentAccountSelectedButton" : "RecentAccountButton");
        }
    }

    private void FocusCredentialInput(bool preferPin = false)
    {
        if (preferPin)
            LoginPinBox.Focus();
        else
            LoginPasswordBox.Focus();
    }

    private void SetSubmittingState(bool isSubmitting)
    {
        _isSubmitting = isSubmitting;
        BtnSubmit.IsEnabled = !isSubmitting;
        LoginPanel.IsEnabled = !isSubmitting;
        SetupPanel.IsEnabled = !isSubmitting;
        SubmitBusyIndicator.Visibility = isSubmitting ? Visibility.Visible : Visibility.Collapsed;
        TxtSubmitLabel.Text = isSubmitting
            ? (_isSetupMode ? "正在进入" : "登录中")
            : (_isSetupMode ? "完成设置并进入" : "登录");
    }

    private void OpenMainWindow()
    {
        var mainWindow = new MainWindow();
        global::System.Windows.Application.Current.MainWindow = mainWindow;
        _openedMainWindow = true;
        mainWindow.Show();
        Close();
    }

    private void LoginWindow_Closed(object? sender, EventArgs e)
    {
        if (!_openedMainWindow)
            global::System.Windows.Application.Current.Shutdown();
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        LoginErrorHost.Visibility = Visibility.Visible;
    }

    private void ClearError()
    {
        TxtError.Text = string.Empty;
        LoginErrorHost.Visibility = Visibility.Collapsed;
    }
}
