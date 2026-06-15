using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsTabLayoutTests
{
    [Fact]
    public void SettingsTab_UsesProductStyleSectionFlowWithoutOverviewDeck()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"SettingsSectionFlow\"", xaml);
        Assert.Contains("x:Name=\"SettingsLocationWeatherSection\"", xaml);
        Assert.Contains("x:Name=\"SettingsStorageSection\"", xaml);
        Assert.Contains("x:Name=\"SettingsSystemSection\"", xaml);
        Assert.Contains("Text=\"外观\"", xaml);
        Assert.Contains("Text=\"系统\"", xaml);
        Assert.Contains("Text=\"位置与天气\"", xaml);
        Assert.Contains("Text=\"存储\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewStrip\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewStatusRows\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsSystemRows\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsGroupDivider\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewPreferenceRow\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewSystemRow\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewHero\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewHeroSurface\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricMatrix\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewCompactSummaryGrid\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricsGrid\"", xaml);
        Assert.DoesNotContain("Text=\"设置概览\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsWorkbenchColumns\"", xaml);
        Assert.DoesNotContain("Text=\"天气与城市\"", xaml);
        Assert.DoesNotContain("Text=\"文件位置\"", xaml);
    }

    [Fact]
    public void AppearanceSettingsPanel_UsesWorkbenchCardWithCompactThemeSelectorsAndThreeFontPresets()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml"));
        var themeCardXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml"));

        Assert.Contains("x:Name=\"AppearanceWorkbenchCard\"", xaml);
        Assert.Contains("x:Name=\"AppearanceSectionDivider\"", xaml);
        Assert.Contains("x:Name=\"AppearanceThemeCompactList\"", xaml);
        Assert.Contains("x:Name=\"AppearanceFlowStack\"", xaml);
        Assert.Contains("Style=\"{StaticResource SettingsInsetCard}\"", xaml);
        Assert.Contains("x:Name=\"RadioFontCompact\"", xaml);
        Assert.Contains("x:Name=\"RadioFontBalanced\"", xaml);
        Assert.Contains("x:Name=\"RadioFontExpanded\"", xaml);
        Assert.Contains("Content=\"A-\"", xaml);
        Assert.Contains("Content=\"A\"", xaml);
        Assert.Contains("Content=\"A+\"", xaml);
        Assert.Contains("MinWidth=\"120\"", themeCardXaml);
        Assert.Contains("MinHeight=\"72\"", themeCardXaml);
        Assert.Contains("x:Name=\"ThemeChoiceDot\"", themeCardXaml);
        Assert.Contains("x:Name=\"ThemeSwatchRow\"", themeCardXaml);
        Assert.Contains("x:Name=\"ThemePreviewField\"", themeCardXaml);
        Assert.Contains("x:Name=\"ThemeFooterRow\"", themeCardXaml);
        Assert.Contains("Width=\"10\"", themeCardXaml);
        Assert.Contains("Height=\"10\"", themeCardXaml);
        Assert.DoesNotContain("Text=\"外观\"", xaml);
        Assert.DoesNotContain("x:Name=\"AppearanceAppSection\"", xaml);
        Assert.DoesNotContain("x:Name=\"PreviewHost\"", themeCardXaml);
        Assert.DoesNotContain("x:Name=\"AppearancePreviewSummaryCard\"", xaml);
    }

    [Fact]
    public void SettingsPanels_UseCardWorkbenchLayout()
    {
        var weatherXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml"));
        var storageXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/StorageLocationsSettingsPanel.xaml"));
        var backupXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/BackupSettingsPanel.xaml"));
        var settingsTabXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"WeatherWorkbenchGrid\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherPreferenceCard\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherPreferenceGrid\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherStatusRail\"", weatherXaml);
        Assert.Contains("x:Name=\"RecommendationStatusRail\"", weatherXaml);
        Assert.Contains("x:Name=\"StorageWorkbenchCard\"", storageXaml);
        Assert.Contains("x:Name=\"StorageRowGrid\"", storageXaml);
        Assert.Contains("Text=\"数据库\"", storageXaml);
        Assert.Contains("Columns=\"2\"", backupXaml);
        Assert.DoesNotContain("Columns=\"3\"", backupXaml);
        Assert.Contains("x:Name=\"SettingsSystemWorkspaceGrid\"", settingsTabXaml);
        Assert.Contains("x:Name=\"SettingsSystemTopRow\"", settingsTabXaml);
        Assert.Contains("x:Name=\"SettingsSystemBottomRow\"", settingsTabXaml);
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
        Assert.DoesNotContain("设置概览", xaml);
        Assert.DoesNotContain("同名大卡", xaml);
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
