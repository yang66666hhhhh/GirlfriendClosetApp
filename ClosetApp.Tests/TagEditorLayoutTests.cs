using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class TagEditorLayoutTests
{
    [Fact]
    public void TagEditor_UsesSharedModalButtons()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Tags/Controls/TagEditorPanel.xaml"));

        Assert.Contains("Style=\"{StaticResource ModalCloseButton}\"", xaml);
        Assert.Contains("Style=\"{StaticResource ModalCancelButton}\"", xaml);
        Assert.Contains("Style=\"{StaticResource ModalSaveButton}\"", xaml);
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
