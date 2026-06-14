using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitHistoryDialogLayoutTests
{
    [Fact]
    public void OutfitHistoryDialog_ProvidesClearHistoryActionWithUnifiedConfirmModal()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/OutfitHistoryDialog.xaml"));
        var codeBehind = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/OutfitHistoryDialog.xaml.cs"));

        Assert.Contains("x:Name=\"ClearHistoryButton\"", xaml);
        Assert.Contains("Content=\"清空记录\"", xaml);
        Assert.Contains("Visibility=\"{Binding HasAnyWornRecords", xaml);
        Assert.Contains("HistoryDangerActionButtonStyle", xaml);
        Assert.Contains("ConfirmModal.ShowAsync", codeBehind);
        Assert.Contains("confirmStyleKey: \"ModalDangerButton\"", codeBehind);
        Assert.DoesNotContain("MessageBox.Show", codeBehind);
    }

    [Fact]
    public void OutfitHistoryDialogs_UseDynamicTypographyTokensForReadableText()
    {
        var historyXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/OutfitHistoryDialog.xaml"));
        var dayDetailsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/WornDayDetailsDialog.xaml"));

        Assert.Contains("FontSize=\"{DynamicResource FontSize.SectionTitle}\"", historyXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Body}\"", historyXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Label}\"", historyXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Meta}\"", historyXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Tiny}\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"8.5\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"9\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"9.5\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"10\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"10.5\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"11\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"11.5\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"12\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"13\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"14\"", historyXaml);
        Assert.DoesNotContain("FontSize=\"16\"", historyXaml);
        Assert.DoesNotContain("Property=\"FontSize\" Value=\"11\"", historyXaml);

        Assert.Contains("FontSize=\"{DynamicResource FontSize.Body}\"", dayDetailsXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Meta}\"", dayDetailsXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Hint}\"", dayDetailsXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Tiny}\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"9.5\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"10\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"10.5\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"11\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"11.5\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"12\"", dayDetailsXaml);
        Assert.DoesNotContain("FontSize=\"14\"", dayDetailsXaml);
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
