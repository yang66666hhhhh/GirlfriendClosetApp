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
    public void OutfitsTab_SecondaryRecommendationRail_UsesCompactHeaderAndUnifiedTagRail()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"SecondaryRecommendationRail\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationCardsHost\"", xaml);
        Assert.Contains("SecondaryRecommendationCardsPanel", xaml);
        Assert.Contains("Width=\"150\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationTagRail\"", xaml);
        Assert.Contains("ItemsSource=\"{Binding CandidateDisplayTags}\"", xaml);
        Assert.Contains("Text=\"{Binding CandidateIndexLabel}\"", xaml);
        Assert.DoesNotContain("ItemsSource=\"{Binding HighlightTags}\"", xaml);
        Assert.DoesNotContain("x:Name=\"SecondaryRecommendationHeader\"", xaml);
        Assert.DoesNotContain("Text=\"{Binding SecondaryWeatherRecommendations.Count, StringFormat=再看 {0} 套}\"", xaml);
        Assert.Contains("Background=\"Transparent\"", xaml);
        Assert.Contains("BorderThickness=\"0\"", xaml);
        Assert.Contains("Padding=\"0\"", xaml);
        Assert.DoesNotContain("Height=\"126\"", xaml);
        Assert.Contains("VerticalAlignment=\"Stretch\"", xaml);
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
        Assert.Contains("MinHeight=\"332\"", xaml);
        Assert.Contains("x:Name=\"SecondaryRecommendationRail\"", xaml);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
        Assert.Contains("MaxHeight=\"24\"", xaml);
        Assert.Contains("<RowDefinition Height=\"*\"/>", xaml);
        Assert.Contains("x:Name=\"TodayHeroCardHost\"", xaml);
        Assert.Contains("Height=\"{Binding ElementName=TodayHeroCardHost, Path=ActualHeight}\"", xaml);
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
