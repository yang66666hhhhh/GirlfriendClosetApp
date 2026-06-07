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
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
        Assert.DoesNotContain(
            "MaxWidth=\"520\"\r\n                            HorizontalAlignment=\"Stretch\"\r\n                            VerticalAlignment=\"Center\"",
            xaml);
    }

    [Fact]
    public void NavigationSidebar_ProfileMenuDoesNotOfferDirectUserSwitching()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.DoesNotContain("BuildUserSwitchMenuHeader", code);
        Assert.DoesNotContain("SwitchUser_Click", code);
        Assert.DoesNotContain("GetAllAsync()", code);
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
