using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitsTabThreadingTests
{
    [Fact]
    public void OutfitsTab_DisplayModeUpdate_MarshalsBackToDispatcher()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml.cs"));

        Assert.Contains("Dispatcher.CheckAccess()", code);
        Assert.Contains("Dispatcher.InvokeAsync(HandleCardDisplayModeChanged", code);
        Assert.Contains("HandleCardDisplayModeChanged();", code);
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
