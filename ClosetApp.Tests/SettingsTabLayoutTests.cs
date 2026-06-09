using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsTabLayoutTests
{
    [Fact]
    public void SettingsTab_UsesCondensedWorkbenchSummaryDeck()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"SettingsOverviewThemeCard\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewDisplayCard\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewSignalsPanel\"", xaml);
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
