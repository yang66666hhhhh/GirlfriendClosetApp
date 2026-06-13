using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClosetApp.Tests;

public class ComboBoxLayoutRulesTests
{
    [Fact]
    public void ComboBox_WithExplicitItemTemplate_DoesNotAlsoUseDisplayMemberPath()
    {
        var xamlFiles = Directory
            .EnumerateFiles(FindProjectDirectory("ClosetApp.UI"), "*.xaml", SearchOption.AllDirectories)
            .ToList();

        var offendingBlocks = new List<string>();

        foreach (var file in xamlFiles)
        {
            var xaml = File.ReadAllText(file);
            var comboBoxes = Regex.Matches(
                xaml,
                @"<ComboBox\b[\s\S]*?</ComboBox>",
                RegexOptions.CultureInvariant);

            foreach (Match comboBox in comboBoxes)
            {
                var block = comboBox.Value;
                if (!block.Contains("<ComboBox.ItemTemplate>", StringComparison.Ordinal) ||
                    !block.Contains("DisplayMemberPath=", StringComparison.Ordinal))
                {
                    continue;
                }

                offendingBlocks.Add($"{Path.GetFileName(file)} => {ExtractFirstLine(block)}");
            }
        }

        Assert.True(
            offendingBlocks.Count == 0,
            "这些 ComboBox 同时使用了 DisplayMemberPath 和 ItemTemplate，容易让选中态退回到对象 ToString()：" +
            Environment.NewLine +
            string.Join(Environment.NewLine, offendingBlocks));
    }

    [Fact]
    public void ObjectBackedComboBoxes_DeclareExplicitDisplayMapping()
    {
        var outfitsTabXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml"));
        var loginWindowXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));
        var weatherPanelXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml"));
        var wardrobeHeaderXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Clothing/WardrobeCollectionHeaderPanel.xaml"));

        Assert.Contains("ItemsSource=\"{Binding SceneFilterOptions}\"", outfitsTabXaml);
        Assert.Contains("ItemTemplate=\"{StaticResource OutfitFilterOptionTemplate}\"", outfitsTabXaml);

        Assert.Contains("ItemsSource=\"{Binding SeasonFilterOptions}\"", outfitsTabXaml);
        Assert.Contains("ItemTemplate=\"{StaticResource OutfitFilterOptionTemplate}\"", outfitsTabXaml);

        Assert.Contains("ItemsSource=\"{Binding SortOptions}\"", outfitsTabXaml);
        Assert.Contains("<ComboBox.ItemTemplate>", outfitsTabXaml);

        Assert.Contains("x:Name=\"RecentAccountSelector\"", loginWindowXaml);
        Assert.Contains("DisplayMemberPath=\"AccountName\"", loginWindowXaml);
        Assert.Contains("TextSearch.TextPath=\"AccountName\"", loginWindowXaml);

        Assert.Contains("ItemsSource=\"{Binding RecommendationSceneOptions}\"", weatherPanelXaml);
        Assert.Contains("DisplayMemberPath=\"Label\"", weatherPanelXaml);

        Assert.Contains("ItemsSource=\"{Binding RecommendationRotationStrategyOptions}\"", weatherPanelXaml);
        Assert.Contains("DisplayMemberPath=\"Label\"", weatherPanelXaml);

        Assert.Contains("ItemsSource=\"{Binding SortOptions}\"", wardrobeHeaderXaml);
        Assert.Contains("<ComboBox.ItemTemplate>", wardrobeHeaderXaml);
    }

    [Fact]
    public void SharedComboBoxTemplate_SeparatesPopupShadowFromRoundedContentSurface()
    {
        var inputsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Inputs.xaml"));

        Assert.Contains("x:Name=\"DropDownShadow\"", inputsXaml);
        Assert.Contains("x:Name=\"DropDownBorder\"", inputsXaml);
        Assert.Contains("x:Name=\"DropDownShadow\"", inputsXaml);
        Assert.Contains("Effect=\"{StaticResource ComboBoxPopupLift}\"", inputsXaml);

        var shadowStart = inputsXaml.IndexOf("x:Name=\"DropDownShadow\"", StringComparison.Ordinal);
        var borderStart = inputsXaml.IndexOf("x:Name=\"DropDownBorder\"", StringComparison.Ordinal);

        Assert.True(shadowStart >= 0 && borderStart > shadowStart, "共享 ComboBox 弹层应先渲染阴影层，再渲染圆角内容层。");

        var borderSection = inputsXaml.Substring(borderStart, Math.Min(420, inputsXaml.Length - borderStart));
        Assert.Contains("<ScrollViewer", borderSection);
    }

    private static string ExtractFirstLine(string block)
    {
        using var reader = new StringReader(block);
        return reader.ReadLine()?.Trim() ?? block.Trim();
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

    private static string FindProjectDirectory(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(directory.FullName, relativePath);
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException($"Cannot locate {relativePath} from {AppContext.BaseDirectory}.");
    }
}
