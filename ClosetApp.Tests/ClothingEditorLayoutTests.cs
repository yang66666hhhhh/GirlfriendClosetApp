using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class ClothingEditorLayoutTests
{
    [Fact]
    public void ClothingEditor_UsesSharedOverlayActionButtons()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Clothing/ClothingEditorPanel.xaml"));

        Assert.Contains("Style=\"{StaticResource GhostButton}\"", xaml);
        Assert.DoesNotContain("x:Key=\"GhostIconBtn\"", xaml);
        Assert.DoesNotContain("<ControlTemplate TargetType=\"Button\">", xaml);
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
