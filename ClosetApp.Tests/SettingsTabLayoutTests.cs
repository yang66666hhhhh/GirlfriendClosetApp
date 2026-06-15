using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsTabLayoutTests
{
    [Fact]
    public void SettingsTab_UsesCondensedWorkbenchSummaryDeck()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"SettingsOverviewStrip\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewStatusRows\"", xaml);
        Assert.Contains("x:Name=\"SettingsSystemRows\"", xaml);
        Assert.Contains("x:Name=\"SettingsGroupDivider\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewPreferenceRow\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewSystemRow\"", xaml);
        Assert.Contains("x:Key=\"SettingsOverviewRowValue\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewHero\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewHeroSurface\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricMatrix\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewCompactSummaryGrid\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricsGrid\"", xaml);
        Assert.Contains("Text=\"设置概览\"", xaml);
        Assert.Contains("x:Name=\"SettingsWorkbenchColumns\"", xaml);
    }

    [Fact]
    public void AppearanceSettingsPanel_UsesWarmMinimalControlsInsteadOfLargeThemePreviews()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml"));
        var themeCardXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml"));

        Assert.Contains("x:Name=\"AppearanceSectionDivider\"", xaml);
        Assert.Contains("x:Name=\"AppearanceThemeCompactList\"", xaml);
        Assert.Contains("x:Name=\"AppearancePreferenceGroup\"", xaml);
        Assert.Contains("x:Name=\"AppearanceInfoGroup\"", xaml);
        Assert.Contains("MinHeight=\"58\"", themeCardXaml);
        Assert.Contains("x:Name=\"ThemeChoiceDot\"", themeCardXaml);
        Assert.Contains("x:Name=\"ThemeSwatchRow\"", themeCardXaml);
        Assert.Contains("Width=\"10\"", themeCardXaml);
        Assert.Contains("Height=\"10\"", themeCardXaml);
        Assert.DoesNotContain("MinHeight=\"172\"", themeCardXaml);
        Assert.DoesNotContain("x:Name=\"PreviewHost\"", themeCardXaml);
        Assert.DoesNotContain("x:Name=\"AppearancePreviewSummaryCard\"", xaml);
    }

    [Fact]
    public void SettingsTab_Overview_RemovesRepeatedDescriptionsAndDuplicateAiSummary()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.DoesNotContain("Text=\"{Binding ThemeDescription}\"", xaml);
        Assert.DoesNotContain("Text=\"{Binding OutfitCardDisplayDetail}\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewQuickFactsPanel\"", xaml);
        Assert.DoesNotContain("Text=\"天气、推荐和图片生成。\"", xaml);
        Assert.DoesNotContain("Text=\"目录、缓存、日志和备份。\"", xaml);
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
