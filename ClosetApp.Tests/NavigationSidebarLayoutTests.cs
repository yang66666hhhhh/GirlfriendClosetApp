using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class NavigationSidebarLayoutTests
{
    [Fact]
    public void NavigationSidebar_AccountCardUsesCompactBalancedLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.Contains("x:Name=\"CurrentUserAvatar\"", xaml);
        Assert.Contains("x:Name=\"ProfileTextPanel\"", xaml);
        Assert.Contains("x:Name=\"ProfileChevronShell\"", xaml);
        Assert.Contains("new Thickness(16, 28, 16, 24)", code);
        Assert.Contains("Margin=\"12,28,12,24\"", xaml);
        Assert.Contains("Padding=\"10\"", xaml);
        Assert.Contains("Width=\"52\"", xaml);
        Assert.Contains("Height=\"52\"", xaml);
        Assert.Contains("MaxWidth=\"104\"", xaml);
        Assert.Contains("Width=\"24\"", xaml);
        Assert.Contains("Height=\"24\"", xaml);
        Assert.Contains("超管", code);
        Assert.Contains("成员", code);
        Assert.DoesNotContain("MaxWidth=\"86\"", xaml);
    }

    [Fact]
    public void NavigationSidebar_CollapsedMode_UsesDockLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));
        var mainWindowCode = File.ReadAllText(FindProjectFile("ClosetApp.UI/MainWindow.xaml.cs"));

        Assert.Contains("x:Name=\"CollapsedProfileButton\"", xaml);
        Assert.Contains("x:Name=\"CollapsedProfileShell\"", xaml);
        Assert.Contains("Width=\"56\"", xaml);
        Assert.Contains("Height=\"56\"", xaml);
        Assert.Contains("x:Name=\"CollapsedCurrentUserAvatar\"", xaml);
        Assert.Contains("x:Name=\"SidebarNavHost\"", xaml);
        Assert.Contains("x:Name=\"SidebarDivider\"", xaml);
        Assert.Contains("x:Name=\"CollapseButtonHost\"", xaml);
        Assert.Contains("x:Name=\"ExpandedLayout\"", xaml);
        Assert.Contains("x:Name=\"DockIcon\"", xaml);
        Assert.Contains("Trigger Property=\"Width\" Value=\"{StaticResource CollapsedNavButtonSize}\"", xaml);
        Assert.Contains("UpdateCollapsedDockState", code);
        Assert.Contains("UpdateNavTooltips", code);
        Assert.Contains("var triggerButton = sender as FrameworkElement ?? BtnProfile;", code);
        Assert.Contains("88.0", mainWindowCode);
        Assert.DoesNotContain("72.0", mainWindowCode);
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
