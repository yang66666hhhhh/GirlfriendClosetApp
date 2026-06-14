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

        Assert.Contains("x:Name=\"SecondaryRecommendationHintText\"", xaml);
        Assert.Contains("Text=\"{Binding SecondaryWeatherRecommendationSectionBody}\"", xaml);
        Assert.Contains("Foreground=\"{DynamicResource TextTertiaryBrush}\"", xaml);
        Assert.DoesNotContain("Text=\"备选搭配\"", xaml);
    }

    [Fact]
    public void OutfitsTab_TodayHeroCard_SeparatesInfoAndActionZones()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));

        Assert.Contains("x:Name=\"TodayHeroCardGrid\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroInfoColumn\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroActionsPanel\"", xaml);
        Assert.Contains("x:Name=\"TodayHeroQuickLinksPanel\"", xaml);
        Assert.Contains("MinHeight=\"320\"", xaml);
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
