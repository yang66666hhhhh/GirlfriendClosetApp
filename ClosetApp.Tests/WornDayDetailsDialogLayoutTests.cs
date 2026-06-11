using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class WornDayDetailsDialogLayoutTests
{
    [Fact]
    public void WornDayDetailsDialog_OutfitPicker_UsesExplicitNameTemplate()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/WornDayDetailsDialog.xaml"));
        var outfitPickerStart = xaml.IndexOf("x:Name=\"OutfitPicker\"", StringComparison.Ordinal);

        Assert.Contains("x:Name=\"OutfitPicker\"", xaml);
        Assert.Contains("<ComboBox.ItemTemplate>", xaml);
        Assert.Contains("Text=\"{Binding Name}\"", xaml);

        var outfitPickerSection = xaml.Substring(outfitPickerStart, Math.Min(320, xaml.Length - outfitPickerStart));
        Assert.DoesNotContain("DisplayMemberPath=\"Name\"", outfitPickerSection);
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
