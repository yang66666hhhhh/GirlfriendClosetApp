using System.IO;
using System.Xml.Linq;
using Xunit;

namespace ClosetApp.Tests;

public class LocalUserManagementDialogLayoutTests
{
    [Theory]
    [InlineData("TextBlock", "Text", "快捷 PIN（可选）")]
    [InlineData("PasswordBox", "x:Name", "NewUserPinBox")]
    [InlineData("Button", "Content", "新增用户")]
    public void NewUserFormControls_AreNotPlacedInSpacerRows(string elementName, string attributeName, string attributeValue)
    {
        var document = XDocument.Load(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));
        var element = FindElement(document, elementName, attributeName, attributeValue);

        Assert.NotNull(element);
        Assert.True(IsInContentRow(element!), $"{elementName} {attributeValue} is assigned to a spacer Grid.Row.");
    }

    [Fact]
    public void UserSurfaces_UseSharedLocalUserAvatarControl()
    {
        Assert.True(
            File.Exists(FindProjectFile("ClosetApp.UI/Components/Shared/LocalUserAvatar.xaml")),
            "The local user avatar should be a shared themed control.");

        Assert.Contains(
            "shared:LocalUserAvatar",
            File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml")));
        Assert.Contains(
            "shared:LocalUserAvatar",
            File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml")));
    }

    private static XElement? FindElement(XDocument document, string elementName, string attributeName, string attributeValue)
    {
        var xamlNamespace = XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml");
        return document
            .Descendants()
            .FirstOrDefault(element =>
                element.Name.LocalName == elementName &&
                string.Equals(
                    GetAttributeValue(element, attributeName, xamlNamespace),
                    attributeValue,
                    StringComparison.Ordinal));
    }

    private static string? GetAttributeValue(XElement element, string attributeName, XNamespace xamlNamespace)
    {
        return attributeName == "x:Name"
            ? element.Attribute(xamlNamespace + "Name")?.Value
            : element.Attribute(attributeName)?.Value;
    }

    private static bool IsInContentRow(XElement element)
    {
        var rowAttribute = element.Attribute("Grid.Row")?.Value;
        if (!int.TryParse(rowAttribute, out var rowIndex))
            return true;

        var grid = element.Ancestors().FirstOrDefault(ancestor => ancestor.Name.LocalName == "Grid");
        var rowDefinitions = grid?
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == "Grid.RowDefinitions")?
            .Elements()
            .Where(child => child.Name.LocalName == "RowDefinition")
            .ToList();

        if (rowDefinitions == null || rowIndex < 0 || rowIndex >= rowDefinitions.Count)
            return false;

        var height = rowDefinitions[rowIndex].Attribute("Height")?.Value;
        return !double.TryParse(height, out var fixedHeight) || fixedHeight >= 30;
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
