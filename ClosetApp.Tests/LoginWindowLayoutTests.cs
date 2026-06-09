using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class LoginWindowLayoutTests
{
    [Fact]
    public void LoginWindow_DoesNotRenderDirectAccountPicker()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.DoesNotContain("x:Name=\"UsersList\"", xaml);
        Assert.DoesNotContain("LoginUserItem", xaml);
        Assert.DoesNotContain("左侧用户只用于快速填入账号", xaml);
    }

    [Fact]
    public void LoginWindow_FormUsesScrollableErrorSafeLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"LoginFormScrollViewer\"", xaml);
        Assert.Contains("x:Name=\"LoginErrorHost\"", xaml);
        Assert.Contains("x:Key=\"LoginSubmitButton\"", xaml);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"52\"/>", xaml);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
        Assert.DoesNotContain(
            "MaxWidth=\"520\"\r\n                            HorizontalAlignment=\"Stretch\"\r\n                            VerticalAlignment=\"Center\"",
            xaml);
    }

    [Fact]
    public void LoginWindow_DefaultSizeLeavesRoomForLoginForm()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("Width=\"1040\"", xaml);
        Assert.Contains("Height=\"720\"", xaml);
        Assert.Contains("MinWidth=\"900\"", xaml);
        Assert.Contains("MinHeight=\"660\"", xaml);
        Assert.DoesNotContain("<Border Margin=\"42\"", xaml);
        Assert.Contains("<Border Margin=\"34\"", xaml);
    }

    [Fact]
    public void LoginWindow_RendersRecentAccountDropdown()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"RecentAccountSelector\"", xaml);
        Assert.Contains("Text=\"账号\" Style=\"{StaticResource LoginInputLabel}\"", xaml);
        Assert.Contains("IsEditable=\"True\"", xaml);
        Assert.Contains("TextSearch.TextPath=\"AccountName\"", xaml);
        Assert.Contains("x:Name=\"TxtSubmitLabel\"", xaml);
        Assert.Contains("x:Name=\"SubmitBusyIndicator\"", xaml);
        Assert.Contains("x:Name=\"WelcomeMetricsCard\"", xaml);
        Assert.Contains("x:Name=\"WelcomeSessionCard\"", xaml);
        Assert.Contains("x:Name=\"LoginErrorIcon\"", xaml);
        Assert.Contains("x:Name=\"TxtErrorTitle\"", xaml);
        Assert.Contains("Background=\"{DynamicResource DangerLightBrush}\"", xaml);
        Assert.DoesNotContain("ContentStringFormat=\"{Binding SelectionBoxItemStringFormat", xaml);
        Assert.DoesNotContain("SelectionBoxItemStringFormat", xaml);
        Assert.DoesNotContain("x:Name=\"LoginAccountBox\"", xaml);
        Assert.DoesNotContain("x:Name=\"RecentAccountsPanel\"", xaml);
        Assert.DoesNotContain("x:Name=\"RecentAccountsHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"SelectedAccountCard\"", xaml);
    }

    [Fact]
    public void LoginWindow_CodeBehindSyncsRecentAccountDropdown()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));

        Assert.Contains("ApplyRecentAccountDropdownState", code);
        Assert.Contains("RecentAccountSelector_SelectionChanged", code);
        Assert.Contains("RecentAccountSelector_TextChanged", code);
        Assert.Contains("GetLoginAccountName()", code);
        Assert.DoesNotContain("LoginAccountBox_TextChanged", code);
        Assert.DoesNotContain("BuildRecentAccountButton", code);
        Assert.DoesNotContain("RecentAccount_Click", code);
        Assert.DoesNotContain("AnimateRecentAccountButtons", code);
    }

    [Fact]
    public void LoginWindow_UsesContextualInlineErrorHandling()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("private void ShowError(string title, string message)", code);
        Assert.Contains("TxtErrorTitle.Text = title;", code);
        Assert.Contains("ShowError(\"启动失败\"", code);
        Assert.Contains("ShowError(_isSetupMode ? \"设置失败\" : \"登录失败\"", code);
        Assert.Contains("ValidateLoginInputs();", code);
        Assert.Contains("ValidateSetupInputs();", code);
        Assert.Contains("请输入账号。", code);
        Assert.Contains("HookInputErrorClearing();", code);
        Assert.Contains("ClearErrorIfUserEditing", code);
        Assert.Contains("BuildInputError(RecentAccountSelector", code);
        Assert.Contains("BuildInputError(LoginPasswordBox", code);
        Assert.Contains("Text=\"账号\" Style=\"{StaticResource LoginInputLabel}\"", xaml);
        Assert.Contains("x:Key=\"LoginFieldErrorText\"", xaml);
        Assert.Contains("x:Name=\"TxtLoginAccountError\"", xaml);
        Assert.Contains("x:Name=\"TxtLoginPasswordError\"", xaml);
        Assert.Contains("x:Name=\"TxtSetupAccountError\"", xaml);
        Assert.Contains("BuildInputError(RecentAccountSelector, TxtLoginAccountError", code);
        Assert.Contains("BuildInputError(LoginPasswordBox, TxtLoginPasswordError", code);
        Assert.Contains("ClearFieldErrors();", code);
        Assert.Contains("InputValidationException", code);
        Assert.Contains("catch (InputValidationException)", code);
    }

    [Fact]
    public void LoginWindow_UsesExplicitCredentialModeSelector()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"CredentialModePanel\"", xaml);
        Assert.Contains("x:Name=\"BtnPasswordMode\"", xaml);
        Assert.Contains("x:Name=\"BtnPinMode\"", xaml);
        Assert.Contains("x:Name=\"PasswordCredentialPanel\"", xaml);
        Assert.Contains("x:Name=\"PinCredentialPanel\"", xaml);
        Assert.Contains("LoginCredentialMode", code);
        Assert.Contains("SetLoginCredentialMode", code);
        Assert.Contains("PasswordMode_Click", code);
        Assert.Contains("PinMode_Click", code);
        Assert.Contains("SelectedCredentialMode == LoginCredentialMode.Pin", code);
    }

    [Fact]
    public void UserManagementDialog_UsesManagementConsoleLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"UserManagerStatsBar\"", xaml);
        Assert.Contains("x:Name=\"TxtUserSearch\"", xaml);
        Assert.Contains("x:Name=\"BtnShowCreateUser\"", xaml);
        Assert.Contains("x:Name=\"CreateUserPanel\"", xaml);
        Assert.Contains("x:Name=\"UserDetailPanel\"", xaml);
        Assert.Contains("Content=\"保存资料\"", xaml);
        Assert.Contains("Content=\"重置凭证\"", xaml);
        Assert.Contains("Content=\"删除用户\"", xaml);
    }

    [Fact]
    public void NavigationSidebar_ProfileMenuDoesNotOfferDirectUserSwitching()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.DoesNotContain("BuildUserSwitchMenuHeader", code);
        Assert.DoesNotContain("SwitchUser_Click", code);
        Assert.DoesNotContain("GetAllAsync()", code);
        Assert.Contains("BuildProfileMenuDivider", code);
    }

    [Fact]
    public void NavigationSidebar_ProfileMenuUsesTypedDividerStyle()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.Contains("ProfileMenuDividerStyle", xaml);
        Assert.Contains("TargetType=\"Separator\"", xaml);
        Assert.Contains("new Separator", code);
    }

    [Fact]
    public void UserManagementDialog_DoesNotOfferSessionSwitching()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs"));

        Assert.DoesNotContain("重新登录", xaml);
        Assert.DoesNotContain("SwitchUser_Click", xaml);
        Assert.DoesNotContain("SwitchUser_Click", code);
        Assert.DoesNotContain("CanSwitch", code);
    }

    private static string FindProjectFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Cannot locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
