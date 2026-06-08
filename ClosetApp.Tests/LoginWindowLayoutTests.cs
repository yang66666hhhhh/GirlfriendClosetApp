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
    public void LoginWindow_RendersRecentAccountQuickAccess()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"RecentAccountsPanel\"", xaml);
        Assert.Contains("x:Name=\"RecentAccountsHost\"", xaml);
        Assert.Contains("Text=\"最近登录\"", xaml);
        Assert.Contains("ItemWidth=\"248\"", xaml);
        Assert.Contains("MaxWidth=\"510\"", xaml);
        Assert.Contains("Text=\"上次登录\"", xaml);
        Assert.Contains("x:Name=\"TxtSubmitLabel\"", xaml);
        Assert.Contains("x:Name=\"SubmitBusyIndicator\"", xaml);
        Assert.Contains("x:Key=\"RecentAccountPrimaryChip\"", xaml);
        Assert.Contains("x:Name=\"WelcomeMetricsCard\"", xaml);
        Assert.Contains("x:Name=\"WelcomeSessionCard\"", xaml);
        Assert.Contains("x:Key=\"RecentAccountHoverAvatar\"", xaml);
        Assert.Contains("x:Key=\"RecentAccountSelectedButton\"", xaml);
        Assert.Contains("x:Name=\"RecentAccountsDivider\"", xaml);
        Assert.Contains("x:Name=\"LoginErrorIcon\"", xaml);
        Assert.Contains("x:Name=\"TxtErrorTitle\"", xaml);
        Assert.Contains("Background=\"{DynamicResource DangerLightBrush}\"", xaml);
    }

    [Fact]
    public void LoginWindow_CodeBehindSyncsSelectedRecentAccount()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));

        Assert.Contains("ApplyRecentAccountSelection", code);
        Assert.Contains("_recentAccountButtons", code);
        Assert.Contains("LoginAccountBox_TextChanged", code);
        Assert.Contains("AnimateRecentAccountButtons", code);
        Assert.Contains("DoubleAnimation", code);
        Assert.Contains("TranslateTransform", code);
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
        Assert.DoesNotContain("new Separator()", code);
        Assert.Contains("BuildProfileMenuDivider", code);
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
