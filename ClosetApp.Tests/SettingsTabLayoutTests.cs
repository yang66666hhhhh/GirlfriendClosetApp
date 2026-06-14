using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsTabLayoutTests
{
    [Fact]
    public void SettingsTab_UsesCondensedWorkbenchSummaryDeck()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"SettingsOverviewHero\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewHeroSurface\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewSummaryGrid\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewMetricMatrix\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewCompactSummaryGrid\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricsGrid\"", xaml);
        Assert.Contains("Text=\"设置概览\"", xaml);
        Assert.Contains("x:Name=\"SettingsWorkbenchColumns\"", xaml);
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
