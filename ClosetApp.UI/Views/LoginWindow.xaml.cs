using System.Windows;
using System.Windows.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

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
    private bool _isSyncingRecentAccountSelection;
    private LoginCredentialMode SelectedCredentialMode { get; set; } = LoginCredentialMode.Password;

    public LoginWindow()
    {
        _startupCoordinator = App.Services.GetRequiredService<AppStartupCoordinator>();
        _localUserService = App.Services.GetRequiredService<ILocalUserService>();
        _localAuthService = App.Services.GetRequiredService<ILocalAuthService>();
        InitializeComponent();
        HookInputErrorClearing();
        RecentAccountSelector.Loaded += RecentAccountSelector_Loaded;
        Loaded += LoginWindow_Loaded;
        Closed += LoginWindow_Closed;
        PreviewKeyDown += LoginWindow_PreviewKeyDown;
    }

    private async void LoginWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ClearError();
            await _startupCoordinator.WaitUntilReadyAsync();
            await RefreshUsersAsync();
        }
        catch (Exception ex)
        {
            ShowError("启动失败", $"应用启动时没能完成本地工作区初始化：{ex.Message}");
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
            ClearFieldErrors();

            if (_isSetupMode)
                ValidateSetupInputs();
            else
                ValidateLoginInputs();

            if (_isSetupMode)
                await CompleteSetupAsync();
            else
                await LoginAsync();
        }
        catch (InputValidationException)
        {
            ClearError();
        }
        catch (Exception ex)
        {
            ShowError(_isSetupMode ? "设置失败" : "登录失败", ex.Message);
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
        var usePin = SelectedCredentialMode == LoginCredentialMode.Pin;
        var secret = usePin ? LoginPinBox.Password : LoginPasswordBox.Password;
        var kind = usePin ? LocalCredentialKind.Pin : LocalCredentialKind.Password;

        var result = await _localAuthService.LoginAsync(GetLoginAccountName(), secret, kind);
        if (!result.Success)
            throw new InvalidOperationException(result.ErrorMessage ?? "登录失败。");

        OpenMainWindow();
    }

    private void ApplyRecentAccountsState()
    {
        var state = LoginRecentAccountsBuilder.Build(_users);
        ApplyRecentAccountDropdownState(state);

        if (!string.IsNullOrWhiteSpace(state.PrefillAccountName))
        {
            RecentAccountSelector.Text = state.PrefillAccountName;
            SelectRecentAccount(state.PrefillAccountName);
        }
        else
        {
            RecentAccountSelector.Text = string.Empty;
            SelectRecentAccount(null);
        }

        LoginPasswordBox.Clear();
        LoginPinBox.Clear();
        SetLoginCredentialMode(LoginCredentialMode.Password);
        FocusCredentialInput();
    }

    private void ApplyRecentAccountDropdownState(LoginRecentAccountsState state)
    {
        _isSyncingRecentAccountSelection = true;
        RecentAccountSelector.ItemsSource = state.RecentAccounts;
        RecentAccountSelector.Visibility = state.HasRecentAccounts ? Visibility.Visible : Visibility.Collapsed;
        _isSyncingRecentAccountSelection = false;
    }

    private void RecentAccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isSyncingRecentAccountSelection ||
            RecentAccountSelector.SelectedItem is not LoginRecentAccountItem account)
            return;

        RecentAccountSelector.Text = account.AccountName;
        LoginPasswordBox.Clear();
        LoginPinBox.Clear();
        SetLoginCredentialMode(account.HasPinCredential ? LoginCredentialMode.Pin : LoginCredentialMode.Password);
        FocusCredentialInput(account.HasPinCredential);
    }

    private void RecentAccountSelector_TextChanged(object sender, TextChangedEventArgs e)
    {
        ClearErrorIfUserEditing();

        if (_isSetupMode || _isSubmitting)
            return;

        var accountName = GetLoginAccountName();
        SelectRecentAccount(string.IsNullOrWhiteSpace(accountName) ? null : accountName);
    }

    private void RecentAccountSelector_Loaded(object sender, RoutedEventArgs e)
    {
        if (RecentAccountSelector.Template.FindName("PART_EditableTextBox", RecentAccountSelector) is TextBox textBox)
            textBox.TextChanged += RecentAccountSelector_TextChanged;
    }

    private void SelectRecentAccount(string? accountName)
    {
        _isSyncingRecentAccountSelection = true;
        RecentAccountSelector.SelectedItem = RecentAccountSelector.Items
            .OfType<LoginRecentAccountItem>()
            .FirstOrDefault(item =>
                !string.IsNullOrWhiteSpace(accountName) &&
                string.Equals(item.AccountName, accountName, StringComparison.OrdinalIgnoreCase));
        _isSyncingRecentAccountSelection = false;
    }

    private string GetLoginAccountName() => RecentAccountSelector.Text.Trim();

    private void FocusCredentialInput(bool preferPin = false)
    {
        if (preferPin || SelectedCredentialMode == LoginCredentialMode.Pin)
            LoginPinBox.Focus();
        else
            LoginPasswordBox.Focus();
    }

    private void PasswordMode_Click(object sender, RoutedEventArgs e)
    {
        SetLoginCredentialMode(LoginCredentialMode.Password);
        FocusCredentialInput();
    }

    private void PinMode_Click(object sender, RoutedEventArgs e)
    {
        SetLoginCredentialMode(LoginCredentialMode.Pin);
        FocusCredentialInput(preferPin: true);
    }

    private void SetLoginCredentialMode(LoginCredentialMode mode)
    {
        SelectedCredentialMode = mode;
        var usePin = SelectedCredentialMode == LoginCredentialMode.Pin;
        PasswordCredentialPanel.Visibility = usePin ? Visibility.Collapsed : Visibility.Visible;
        PinCredentialPanel.Visibility = usePin ? Visibility.Visible : Visibility.Collapsed;
        BtnPasswordMode.Style = (Style)FindResource(usePin
            ? "LoginCredentialModeButton"
            : "LoginCredentialModeSelectedButton");
        BtnPinMode.Style = (Style)FindResource(usePin
            ? "LoginCredentialModeSelectedButton"
            : "LoginCredentialModeButton");
        ClearFieldError(TxtLoginPasswordError);
    }

    private void HookInputErrorClearing()
    {
        SetupAccountBox.TextChanged += ClearErrorIfUserEditing;
        LoginPasswordBox.PasswordChanged += ClearErrorIfUserEditing;
        LoginPinBox.PasswordChanged += ClearErrorIfUserEditing;
        SetupPasswordBox.PasswordChanged += ClearErrorIfUserEditing;
        SetupConfirmPasswordBox.PasswordChanged += ClearErrorIfUserEditing;
        SetupPinBox.PasswordChanged += ClearErrorIfUserEditing;
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

    private void ValidateSetupInputs()
    {
        if (string.IsNullOrWhiteSpace(SetupAccountBox.Text))
            throw BuildInputError(SetupAccountBox, TxtSetupAccountError, "请输入管理员账号。");

        if (string.IsNullOrWhiteSpace(SetupPasswordBox.Password))
            throw BuildInputError(SetupPasswordBox, TxtSetupPasswordError, "请输入管理员密码。");

        if (string.IsNullOrWhiteSpace(SetupConfirmPasswordBox.Password))
            throw BuildInputError(SetupConfirmPasswordBox, TxtSetupConfirmPasswordError, "请再次确认管理员密码。");
    }

    private void ValidateLoginInputs()
    {
        if (string.IsNullOrWhiteSpace(GetLoginAccountName()))
            throw BuildInputError(RecentAccountSelector, TxtLoginAccountError, "请输入账号。");

        var hasPassword = !string.IsNullOrWhiteSpace(LoginPasswordBox.Password);
        var hasPin = !string.IsNullOrWhiteSpace(LoginPinBox.Password);
        if (SelectedCredentialMode == LoginCredentialMode.Pin)
        {
            if (!hasPin)
                throw BuildInputError(LoginPinBox, TxtLoginPasswordError, "请输入该账号的 PIN。");

            return;
        }

        if (!hasPassword)
            throw BuildInputError(LoginPasswordBox, TxtLoginPasswordError, "请输入该账号的密码。");
    }

    private static InputValidationException BuildInputError(Control target, TextBlock errorText, string message)
    {
        SetFieldError(errorText, message);
        target.Focus();
        return new InputValidationException(message);
    }

    private static void SetFieldError(TextBlock errorText, string message)
    {
        errorText.Text = message;
        errorText.Visibility = Visibility.Visible;
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

    private void ShowError(string title, string message)
    {
        TxtErrorTitle.Text = title;
        TxtError.Text = message;
        LoginErrorHost.Visibility = Visibility.Visible;
    }

    private void ClearError()
    {
        TxtErrorTitle.Text = _isSetupMode ? "设置失败" : "登录失败";
        TxtError.Text = string.Empty;
        LoginErrorHost.Visibility = Visibility.Collapsed;
    }

    private void ClearErrorIfUserEditing(object? sender = null, RoutedEventArgs? e = null)
    {
        if (_isSubmitting)
            return;

        ClearError();
        ClearFieldErrors();
    }

    private void ClearFieldErrors()
    {
        ClearFieldError(TxtSetupAccountError);
        ClearFieldError(TxtSetupPasswordError);
        ClearFieldError(TxtSetupConfirmPasswordError);
        ClearFieldError(TxtLoginAccountError);
        ClearFieldError(TxtLoginPasswordError);
    }

    private static void ClearFieldError(TextBlock errorText)
    {
        errorText.Text = string.Empty;
        errorText.Visibility = Visibility.Collapsed;
    }

    private sealed class InputValidationException : InvalidOperationException
    {
        public InputValidationException(string message)
            : base(message)
        {
        }
    }

    private enum LoginCredentialMode
    {
        Password,
        Pin
    }
}
