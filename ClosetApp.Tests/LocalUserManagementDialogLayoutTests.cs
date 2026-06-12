using System.IO;
using System.Xml.Linq;
using Xunit;

namespace ClosetApp.Tests;

public class LocalUserManagementDialogLayoutTests
{
    [Theory]
    [InlineData("TextBlock", "Text", "PIN（可选）")]
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

    [Fact]
    public void LocalUserAvatar_KeepsInitialInsideSafeContentBounds()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/LocalUserAvatar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/LocalUserAvatar.xaml.cs"));

        Assert.Contains("x:Name=\"AvatarContentHost\"", xaml);
        Assert.Contains("x:Name=\"AvatarSurface\"", xaml);
        Assert.Contains("Margin=\"2\"", xaml);
        Assert.Contains("Viewbox x:Name=\"InitialViewbox\"", xaml);
        Assert.Contains("Margin=\"1\"", xaml);
        Assert.Contains("x:Name=\"AvatarPhoto\"", xaml);
        Assert.Contains("Margin=\"1.5\"", xaml);
        Assert.Contains("Stretch = Stretch.UniformToFill", code);
        Assert.Contains("AlignmentY = AlignmentY.Center", code);
        Assert.DoesNotContain("OpacityMask", xaml);
        Assert.DoesNotContain("x:Name=\"PhotoMask\"", xaml);
        Assert.DoesNotContain("Viewbox Width=\"48\"", xaml);
        Assert.DoesNotContain("Height=\"48\"", xaml);
    }

    [Fact]
    public void NavigationSidebar_ProfileAvatarHasEnoughRoom()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));

        Assert.Contains("x:Name=\"CurrentUserAvatar\"", xaml);
        Assert.Contains("Width=\"56\"", xaml);
        Assert.Contains("Height=\"56\"", xaml);
        Assert.Contains("ShowStatus=\"False\"", xaml);
        Assert.Contains("MinHeight=\"116\"", xaml);
        Assert.Contains("Padding=\"12\"", xaml);
        Assert.Contains("x:Name=\"ProfileCountBadge\"", xaml);
        Assert.Contains("x:Name=\"TxtClothingCount\"", xaml);
        Assert.Contains("Text=\"0 件衣服\"", xaml);
        Assert.Contains("x:Name=\"TxtCurrentUserRole\"", xaml);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\"", xaml);
        Assert.Contains("x:Name=\"ProfileIdentitySurface\"", xaml);
        Assert.DoesNotContain("x:Name=\"ProfileTouchHintCard\"", xaml);
        Assert.DoesNotContain("Width=\"44\"\r\n                                                Height=\"44\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_ExposesAvatarUploadActions()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs"));

        Assert.Contains("x:Name=\"SelectedUserAvatar\"", xaml);
        Assert.Contains("Content=\"更换头像\"", xaml);
        Assert.Contains("Content=\"移除头像\"", xaml);
        Assert.Contains("SelectAvatar_Click", xaml);
        Assert.Contains("RemoveAvatar_Click", xaml);
        Assert.Contains("SelectAvatar_Click", code);
        Assert.Contains("RemoveAvatar_Click", code);
    }

    [Fact]
    public void UserManagementDialog_UsesStackedCreateUserWorkbench()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"CreateUserFormGrid\"", xaml);
        Assert.Contains("x:Name=\"CreateUserCredentialGrid\"", xaml);
        Assert.Contains("Content=\"创建用户\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_UsesWorkbenchActionSections()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"SelectedUserAccountSection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserSecuritySection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserDangerSection\"", xaml);
        Assert.Contains("x:Name=\"UserDirectorySearchCard\"", xaml);
        Assert.Contains("Style=\"{StaticResource WorkbenchTextInput}\"", xaml);
        Assert.Contains("Style=\"{StaticResource WorkbenchPasswordInput}\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_UsesCurrentUserWorkbenchLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"CurrentSessionBar\"", xaml);
        Assert.Contains("x:Name=\"CurrentSessionAvatar\"", xaml);
        Assert.Contains("x:Name=\"TxtCurrentSessionUser\"", xaml);
        Assert.Contains("x:Name=\"TxtCurrentSessionContext\"", xaml);
        Assert.Contains("x:Name=\"MemberManagementCard\"", xaml);
        Assert.Contains("x:Name=\"MemberSectionHeader\"", xaml);
        Assert.Contains("x:Name=\"MemberListActionBar\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserWorkbench\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserHeroCard\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserAccountSection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserSecuritySection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserDangerSection\"", xaml);
        Assert.DoesNotContain("x:Name=\"UserManagerStatsBar\"", xaml);
        Assert.DoesNotContain("x:Name=\"UserManagerToolbar\"", xaml);
        Assert.DoesNotContain("x:Name=\"CurrentUserWorkbenchCard\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_HasSingleCreateUserEntryPoint()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"BtnCreateUserInline\"", xaml);
        Assert.DoesNotContain("x:Name=\"BtnShowCreateUserToolbar\"", xaml);
        Assert.DoesNotContain("x:Name=\"BtnShowCreateUser\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_UsesTighterHeroAndMoreOpenMemberWorkspace()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.DoesNotContain("先维护当前账号，再管理其他本地成员与独立衣柜工作区。", xaml);
        Assert.DoesNotContain("这里先处理自己的头像、资料和登录凭证。", xaml);
        Assert.DoesNotContain("新增用户只放在这里，避免重复入口。", xaml);
        Assert.Contains("Height=\"756\"", xaml);
        Assert.Contains("Margin=\"28,18,28,0\"", xaml);
        Assert.Contains("<RowDefinition Height=\"18\"/>", xaml);
        Assert.Contains("Padding=\"26,26,26,32\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_PrioritizesDetailWorkspaceOverHeroHeight()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("Height=\"756\"", xaml);
        Assert.Contains("Width=\"34\"", xaml);
        Assert.Contains("FontSize=\"24\"", xaml);
        Assert.Contains("<ColumnDefinition Width=\"300\"/>", xaml);
        Assert.Contains("<ColumnDefinition Width=\"32\"/>", xaml);
        Assert.Contains("<ColumnDefinition Width=\"*\"/>", xaml);
        Assert.Contains("Padding=\"26,26,26,32\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_UsesSelectedUserWorkbenchLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"CurrentSessionBar\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserWorkbench\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserHeroCard\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserAccountSection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserSecuritySection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserDangerSection\"", xaml);
        Assert.DoesNotContain("x:Name=\"CurrentUserWorkbenchCard\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_GivesMoreSpaceToSelectedUserDetail()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("<ColumnDefinition Width=\"300\"/>", xaml);
        Assert.Contains("<ColumnDefinition Width=\"32\"/>", xaml);
        Assert.Contains("<ColumnDefinition Width=\"*\"/>", xaml);
        Assert.Contains("Padding=\"26,26,26,32\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_TopLevelSections_AreNotPlacedInSpacerRows()
    {
        var document = XDocument.Load(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        var currentWorkbench = FindElement(document, "Border", "x:Name", "CurrentSessionBar");
        var memberManagement = FindElement(document, "Border", "x:Name", "MemberManagementCard");
        var detailRegion = FindElement(document, "StackPanel", "x:Name", "SelectedUserWorkbench");

        Assert.NotNull(currentWorkbench);
        Assert.NotNull(memberManagement);
        Assert.NotNull(detailRegion);
        Assert.True(IsInContentRow(currentWorkbench!), "CurrentSessionBar is assigned to a spacer Grid.Row.");
        Assert.True(IsInContentRow(memberManagement!), "MemberManagementCard is assigned to a spacer Grid.Row.");
        Assert.True(IsInContentRow(detailRegion!), "Selected user workbench is assigned to a spacer Grid.Row.");
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
