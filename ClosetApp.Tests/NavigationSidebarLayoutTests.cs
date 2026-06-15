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
    public void NavigationSidebar_ExpandedOnlyLayout_NoCollapseElements()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        // Collapse elements must not exist
        Assert.DoesNotContain("x:Name=\"CollapseButtonHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"CollapsedProfileButton\"", xaml);
        Assert.DoesNotContain("x:Name=\"CollapsedProfileShell\"", xaml);
        Assert.DoesNotContain("x:Name=\"CollapsedCurrentUserAvatar\"", xaml);
        Assert.DoesNotContain("x:Name=\"DockLayout\"", xaml);
        Assert.DoesNotContain("CollapseBtnStyle", xaml);
        Assert.DoesNotContain("CollapsedNavButtonSize", xaml);
        Assert.DoesNotContain("UpdateCollapsedDockState", code);
        Assert.DoesNotContain("ToggleCollapse", code);
        Assert.DoesNotContain("CollapseStateChanged", code);

        // Core expanded elements must still exist
        Assert.Contains("x:Name=\"SidebarNavHost\"", xaml);
        Assert.Contains("x:Name=\"BtnProfile\"", xaml);
        Assert.Contains("x:Name=\"CurrentUserAvatar\"", xaml);
        Assert.Contains("x:Name=\"ProfileTextPanel\"", xaml);
        Assert.Contains("x:Name=\"ProfileCountBadge\"", xaml);
        Assert.Contains("x:Name=\"ExpandedLayout\"", xaml);
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
