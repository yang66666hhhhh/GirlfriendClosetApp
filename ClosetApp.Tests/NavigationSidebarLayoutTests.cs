using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class NavigationSidebarLayoutTests
{
    [Fact]
    public void NavigationSidebar_UsesWorkspaceShellHeaderAndLightIdentityBlock()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));
        var mainWindowXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/MainWindow.xaml"));
        var mainWindowCode = File.ReadAllText(FindProjectFile("ClosetApp.UI/MainWindow.xaml.cs"));

        Assert.Contains("x:Name=\"SidebarWorkspaceHeader\"", xaml);
        Assert.Contains("Text=\"我的衣橱\"", xaml);
        Assert.Contains("x:Name=\"SidebarWorkspaceTitle\"", xaml);
        Assert.Contains("x:Name=\"CurrentUserAvatar\"", xaml);
        Assert.Contains("x:Name=\"ProfileTextPanel\"", xaml);
        Assert.Contains("Text=\"16件衣服\"", xaml);
        Assert.Contains("Width=\"180\"", xaml);
        Assert.Contains("Width=\"180\"", mainWindowXaml);
        Assert.Contains("ExpandedSidebarWidth = 180", code);
        Assert.Contains("180.0", mainWindowCode);
        Assert.Contains("new Thickness(16, 18, 16, 20)", code);
        Assert.Contains("Margin=\"16,18,16,20\"", xaml);
        Assert.Contains("Padding=\"0\"", xaml);
        Assert.Contains("MinHeight=\"64\"", xaml);
        Assert.Contains("Width=\"42\"", xaml);
        Assert.Contains("Height=\"42\"", xaml);
        Assert.Contains("MaxWidth=\"116\"", xaml);
        Assert.Contains("x:Name=\"SidebarCollapseButton\"", xaml);
        Assert.Contains("Width=\"32\"", xaml);
        Assert.Contains("Height=\"32\"", xaml);
        Assert.Contains("x:Name=\"CollapseGlyph\"", xaml);
        Assert.Contains("SidebarCollapseButton.ToolTip", code);
        Assert.Contains("CollapseGlyph.Text = _isCollapsed ? \"▶\" : \"◀\";", code);
        Assert.DoesNotContain("x:Name=\"TxtCurrentUserRole\"", xaml);
        Assert.DoesNotContain("超级管理员", xaml);
        Assert.DoesNotContain("TxtCurrentUserRole.Text", code);
        Assert.DoesNotContain("超管", code);
        Assert.DoesNotContain("成员", code);
        Assert.DoesNotContain("x:Name=\"ProfileIdentitySurface\"", xaml);
        Assert.DoesNotContain("x:Name=\"ProfileChevronShell\"", xaml);
        Assert.DoesNotContain("x:Name=\"CollapseButtonHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"BtnCollapse\"", xaml);
        Assert.DoesNotContain("x:Name=\"ProfileCountBadge\"", xaml);
        Assert.DoesNotContain("x:Name=\"SidebarAiEntry\"", xaml);
        Assert.DoesNotContain("AiEntryRequested", code);
        Assert.DoesNotContain("AiEntryRequested", mainWindowXaml);
    }

    [Fact]
    public void NavigationSidebar_SelectedItemUsesQuietIndicatorInsteadOfHeavyPill()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.Contains("<sys:Double x:Key=\"NavHeight\">44</sys:Double>", xaml);
        Assert.Contains("x:Name=\"NavActiveIndicator\"", xaml);
        Assert.Contains("Width=\"3\"", xaml);
        Assert.Contains("CornerRadius=\"2\"", xaml);
        Assert.Contains("PrimaryLightBrush", xaml);
        Assert.Contains("ApplyNavExpandedMode", code);
        Assert.Contains("Padding=\"8,10\"", xaml);
        Assert.Contains("Margin=\"0,2,0,2\"", xaml);
        Assert.DoesNotContain("<Setter Property=\"Foreground\" Value=\"White\"/>", xaml);
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
        Assert.Contains("x:Name=\"SidebarCollapseButton\"", xaml);
        Assert.Contains("x:Name=\"ExpandedLayout\"", xaml);
        Assert.Contains("x:Name=\"DockIcon\"", xaml);
        Assert.Contains("Trigger Property=\"Width\" Value=\"{StaticResource CollapsedNavButtonSize}\"", xaml);
        Assert.Contains("UpdateCollapsedDockState", code);
        Assert.Contains("UpdateNavTooltips", code);
        Assert.Contains("var triggerButton = sender as FrameworkElement ?? BtnProfile;", code);
        Assert.Contains("88.0", mainWindowCode);
        Assert.DoesNotContain("72.0", mainWindowCode);
        Assert.DoesNotContain("x:Name=\"CollapseButtonHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"SidebarAiEntry\"", xaml);
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
