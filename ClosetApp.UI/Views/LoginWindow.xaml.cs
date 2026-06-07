using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Views;

public partial class LoginWindow : Window
{
    private readonly AppStartupCoordinator _startupCoordinator;
    private readonly ILocalUserService _localUserService;
    private readonly ILocalAuthService _localAuthService;
    private bool _isSetupMode;
    private bool _openedMainWindow;
    private LocalUser? _superAdmin;

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
        var users = await _localUserService.GetAllAsync();
        _superAdmin = users.FirstOrDefault(user => user.Role == LocalUserRole.SuperAdmin);
        _isSetupMode = _superAdmin != null && !_superAdmin.HasPasswordCredential;

        var visibleUsers = _isSetupMode && _superAdmin != null
            ? [_superAdmin]
            : users.ToList();

        UsersList.ItemsSource = visibleUsers.Select(user => new LoginUserRow(user)).ToList();
        UsersList.SelectedIndex = 0;

        SetupPanel.Visibility = _isSetupMode ? Visibility.Visible : Visibility.Collapsed;
        LoginPanel.Visibility = _isSetupMode ? Visibility.Collapsed : Visibility.Visible;
        UsersList.IsEnabled = !_isSetupMode;
        TxtModeTitle.Text = _isSetupMode ? "首次设置管理员密码" : "登录";
        TxtModeDescription.Text = _isSetupMode
            ? "旧数据已归属超级管理员。先设置本机密码，之后每次启动都需要登录。"
            : "输入账号和密码，进入对应衣柜工作区。";
        TxtSubtitle.Text = _isSetupMode
            ? "完成管理员账号密码后，再创建或管理其它本地用户。"
            : "左侧用户只用于快速填入账号；登录仍以账号密码区分用户。";
        BtnSubmit.Content = _isSetupMode ? "完成设置并进入" : "登录";
        if (_isSetupMode && _superAdmin != null)
            SetupAccountBox.Text = string.IsNullOrWhiteSpace(_superAdmin.AccountName)
                ? "admin"
                : _superAdmin.AccountName;
        UpdateSelectedUserText();
        (_isSetupMode ? SetupAccountBox : LoginAccountBox).Focus();
    }

    private void LoginWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || BtnSubmit.IsEnabled == false)
            return;

        Submit_Click(BtnSubmit, new RoutedEventArgs());
        e.Handled = true;
    }

    private void UsersList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectedUserText();
        ClearError();
        if (UsersList.SelectedItem is LoginUserRow row)
            LoginAccountBox.Text = row.AccountName;
        LoginPasswordBox.Clear();
        LoginPinBox.Clear();
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            BtnSubmit.IsEnabled = false;
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
            BtnSubmit.IsEnabled = true;
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

    private void UpdateSelectedUserText()
    {
        if (UsersList.SelectedItem is LoginUserRow row)
        {
            TxtSelectedUser.Text = $"账号 · {row.DisplayName}";
            if (string.IsNullOrWhiteSpace(LoginAccountBox.Text))
                LoginAccountBox.Text = row.AccountName;
        }
    }

    private void ShowError(string message)
    {
        TxtError.Text = message;
        TxtError.Visibility = Visibility.Visible;
    }

    private void ClearError()
    {
        TxtError.Text = string.Empty;
        TxtError.Visibility = Visibility.Collapsed;
    }

    private sealed class LoginUserRow
    {
        public LoginUserRow(LocalUser user)
        {
            User = user;
        }

        public LocalUser User { get; }
        public Guid Id => User.Id;
        public string AccountName => User.AccountName;
        public string DisplayName => User.DisplayName;
        public string? AvatarPath => User.AvatarPhotoPath;
        public string AvatarInitial => string.IsNullOrWhiteSpace(DisplayName) ? "衣" : DisplayName.Trim()[0].ToString();
        public string SummaryText => User.Role == LocalUserRole.SuperAdmin
            ? "超级管理员"
            : User.HasPasswordCredential ? "普通用户" : "普通用户 · 未设置密码";
        public string AccountHint => $"账号 {AccountName}";
    }
}
