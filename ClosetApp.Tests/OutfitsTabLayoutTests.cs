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
