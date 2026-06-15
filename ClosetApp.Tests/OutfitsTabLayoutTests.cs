using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitsTabLayoutTests
{
    [Fact]
    public void OutfitsTab_UsesUnifiedBrowsingWorkbench()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"OutfitsBrowseWorkbench\"", xaml);
        Assert.Contains("x:Name=\"OutfitsToolbarSurface\"", xaml);
        Assert.Contains("x:Name=\"OutfitsFilterRail\"", xaml);
        Assert.Contains("x:Name=\"OutfitsDisplayActionRail\"", xaml);
        Assert.Contains("x:Name=\"OutfitsSortComboBox\"", xaml);
        Assert.Contains("x:Name=\"OutfitsToolbarDivider\"", xaml);
    }

    [Fact]
    public void OutfitsTab_SecondaryRecommendationHint_UsesLightweightTextHint()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"SecondaryRecommendationRail\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationHeader\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationCardsHost\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationHintText\"", xaml);
        Assert.Contains("Text=\"{Binding SecondaryWeatherRecommendationSectionBody}\"", xaml);
        Assert.Contains("Text=\"{Binding SecondaryWeatherRecommendations.Count, StringFormat=再看 {0} 套}\"", xaml);
        Assert.DoesNotContain("Foreground=\"{DynamicResource TextTertiaryBrush}\"", xaml);
    }

    [Fact]
    public void OutfitsTab_SecondaryRecommendationCards_UseUnifiedTagRail()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"SecondaryRecommendationTagRail\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding DisplayReasonTags}\"", xaml);
        Assert.Contains("MaxHeight=\"28\"", xaml);
        Assert.Contains("Height=\"148\"", xaml);
        Assert.DoesNotContain("Text=\"{Binding WearSummaryText}\"", xaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding HighlightTags}\"", xaml);
    }

    [Fact]
    public void OutfitsTab_RecommendationColumns_ShareVisualHeightAnchors()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"TodayRecommendationWorkspace\"", xaml);
        Assert.Contains("x:Name=\"PrimaryRecommendationCardShell\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationRail\"", xaml);
        Assert.Contains("Height=\"148\"", xaml);
        Assert.Contains("MinHeight=\"336\"", xaml);
    }

    [Fact]
    public void OutfitsTab_TodayHeroCard_SeparatesInfoAndActionZones()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"TodayHeroCardGrid\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroInfoColumn\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroStatusPanelHost\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroActionsPanel\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroPrimaryActionRow\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroFooterRailHost\"", xaml);
        Assert.Contains("<WrapPanel x:Name=\"TodayHeroFooterRailHost\"", xaml);
        Assert.DoesNotContain("<Border x:Name=\"TodayHeroFooterRailHost\"", xaml);
        Assert.Contains("MinHeight=\"336\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationRail\"", xaml);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
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
