using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class LoginWindowLayoutTests
{
    [Fact]
    public void LoginWindow_DoesNotRenderDirectAccountPicker()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.DoesNotContain("x:Name=\"UsersList\"", xaml);
        Assert.DoesNotContain("LoginUserItem", xaml);
        Assert.DoesNotContain("左侧用户只用于快速填入账号", xaml);
    }

    [Fact]
    public void LoginWindow_FormUsesScrollableErrorSafeLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"LoginFormScrollViewer\"", xaml);
        Assert.Contains("x:Name=\"LoginErrorHost\"", xaml);
        Assert.Contains("x:Key=\"LoginSubmitButton\"", xaml);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"52\"/>", xaml);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
        Assert.DoesNotContain(
            "MaxWidth=\"520\"\r\n                            HorizontalAlignment=\"Stretch\"\r\n                            VerticalAlignment=\"Center\"",
            xaml);
    }

    [Fact]
    public void LoginWindow_DefaultSizeLeavesRoomForLoginForm()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("Width=\"1040\"", xaml);
        Assert.Contains("Height=\"720\"", xaml);
        Assert.Contains("MinWidth=\"900\"", xaml);
        Assert.Contains("MinHeight=\"660\"", xaml);
        Assert.DoesNotContain("<Border Margin=\"42\"", xaml);
        Assert.Contains("<Border Margin=\"34\"", xaml);
    }

    [Fact]
    public void LoginWindow_RendersRecentAccountDropdown()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Key=\"LoginHeroInfoCard\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource WorkbenchTextInput}\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource WorkbenchPasswordInput}\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource PrimaryButton}\"", xaml);
        Assert.Contains("x:Name=\"RecentAccountSelector\"", xaml);
        Assert.Contains("Text=\"账号\" Style=\"{StaticResource LoginInputLabel}\"", xaml);
        Assert.Contains("IsEditable=\"True\"", xaml);
        Assert.Contains("TextSearch.TextPath=\"AccountName\"", xaml);
        Assert.Contains("x:Name=\"TxtSubmitLabel\"", xaml);
        Assert.Contains("x:Name=\"SubmitBusyIndicator\"", xaml);
        Assert.Contains("x:Name=\"WelcomeMetricsCard\"", xaml);
        Assert.Contains("x:Name=\"WelcomeSessionCard\"", xaml);
        Assert.Contains("x:Name=\"LoginErrorIcon\"", xaml);
        Assert.Contains("x:Name=\"TxtErrorTitle\"", xaml);
        Assert.Contains("Background=\"{DynamicResource DangerLightBrush}\"", xaml);
        Assert.DoesNotContain("ContentStringFormat=\"{Binding SelectionBoxItemStringFormat", xaml);
        Assert.DoesNotContain("SelectionBoxItemStringFormat", xaml);
        Assert.DoesNotContain("x:Name=\"LoginAccountBox\"", xaml);
        Assert.DoesNotContain("x:Name=\"RecentAccountsPanel\"", xaml);
        Assert.DoesNotContain("x:Name=\"RecentAccountsHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"SelectedAccountCard\"", xaml);
        Assert.Contains("x:Name=\"WelcomeInsightStack\"", xaml);
        Assert.Contains("x:Name=\"WelcomeAccountIdentityCard\"", xaml);
        Assert.Contains("Text=\"账号工作区\"", xaml);
    }

    [Fact]
    public void LoginWindow_CodeBehindSyncsRecentAccountDropdown()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));

        Assert.Contains("ApplyRecentAccountDropdownState", code);
        Assert.Contains("RecentAccountSelector_SelectionChanged", code);
        Assert.Contains("RecentAccountSelector_TextChanged", code);
        Assert.Contains("GetLoginAccountName()", code);
        Assert.DoesNotContain("LoginAccountBox_TextChanged", code);
        Assert.DoesNotContain("BuildRecentAccountButton", code);
        Assert.DoesNotContain("RecentAccount_Click", code);
        Assert.DoesNotContain("AnimateRecentAccountButtons", code);
    }

    [Fact]
    public void LoginWindow_UsesContextualInlineErrorHandling()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("private void ShowError(string title, string message)", code);
        Assert.Contains("TxtErrorTitle.Text = title;", code);
        Assert.Contains("ShowError(\"启动失败\"", code);
        Assert.Contains("ShowError(_isSetupMode ? \"设置失败\" : \"登录失败\"", code);
        Assert.Contains("ValidateLoginInputs();", code);
        Assert.Contains("ValidateSetupInputs();", code);
        Assert.Contains("请输入账号。", code);
        Assert.Contains("HookInputErrorClearing();", code);
        Assert.Contains("ClearErrorIfUserEditing", code);
        Assert.Contains("BuildInputError(RecentAccountSelector", code);
        Assert.Contains("BuildInputError(LoginPasswordBox", code);
        Assert.Contains("Text=\"账号\" Style=\"{StaticResource LoginInputLabel}\"", xaml);
        Assert.Contains("x:Key=\"LoginFieldErrorText\"", xaml);
        Assert.Contains("x:Name=\"TxtLoginAccountError\"", xaml);
        Assert.Contains("x:Name=\"TxtLoginPasswordError\"", xaml);
        Assert.Contains("x:Name=\"TxtSetupAccountError\"", xaml);
        Assert.Contains("BuildInputError(RecentAccountSelector, TxtLoginAccountError", code);
        Assert.Contains("BuildInputError(LoginPasswordBox, TxtLoginPasswordError", code);
        Assert.Contains("ClearFieldErrors();", code);
        Assert.Contains("InputValidationException", code);
        Assert.Contains("catch (InputValidationException)", code);
    }

    [Fact]
    public void LoginWindow_UsesExplicitCredentialModeSelector()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"CredentialModePanel\"", xaml);
        Assert.Contains("x:Name=\"BtnPasswordMode\"", xaml);
        Assert.Contains("x:Name=\"BtnPinMode\"", xaml);
        Assert.Contains("Style=\"{StaticResource AppSegmentedTabShell}\"", xaml);
        Assert.Contains("Style=\"{StaticResource AppSegmentedTabButton}\"", xaml);
        Assert.Contains("UniformGrid Columns=\"2\"", xaml);
        Assert.Contains("Checked=\"PasswordMode_Checked\"", xaml);
        Assert.Contains("Checked=\"PinMode_Checked\"", xaml);
        Assert.Contains("x:Name=\"PasswordCredentialPanel\"", xaml);
        Assert.Contains("x:Name=\"PinCredentialPanel\"", xaml);
        Assert.Contains("LoginCredentialMode", code);
        Assert.Contains("SetLoginCredentialMode", code);
        Assert.Contains("PasswordMode_Checked", code);
        Assert.Contains("PinMode_Checked", code);
        Assert.Contains("SelectedCredentialMode == LoginCredentialMode.Pin", code);
    }

    [Fact]
    public void UserManagementDialog_UsesManagementConsoleLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"UserManagerStatsBar\"", xaml);
        Assert.Contains("x:Name=\"UserManagerToolbar\"", xaml);
        Assert.Contains("x:Name=\"UserManagerWorkspaceCard\"", xaml);
        Assert.Contains("x:Name=\"TxtUserSearch\"", xaml);
        Assert.Contains("x:Name=\"BtnShowCreateUser\"", xaml);
        Assert.Contains("x:Name=\"CreateUserPanel\"", xaml);
        Assert.Contains("x:Name=\"UserDetailPanel\"", xaml);
        Assert.Contains("Content=\"保存资料\"", xaml);
        Assert.Contains("Content=\"重置凭证\"", xaml);
        Assert.Contains("Content=\"删除用户\"", xaml);
    }

    [Fact]
    public void NavigationSidebar_ProfileMenuDoesNotOfferDirectUserSwitching()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.DoesNotContain("BuildUserSwitchMenuHeader", code);
        Assert.DoesNotContain("SwitchUser_Click", code);
        Assert.DoesNotContain("GetAllAsync()", code);
        Assert.Contains("BuildProfileMenuDivider", code);
    }

    [Fact]
    public void NavigationSidebar_ProfileMenuUsesTypedDividerStyle()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.Contains("ProfileMenuDividerStyle", xaml);
        Assert.Contains("TargetType=\"MenuItem\"", xaml);
        Assert.Contains("return new MenuItem", code);
        Assert.Contains("Header = divider", code);
        Assert.DoesNotContain("Separator", code);
        Assert.DoesNotContain("ItemContainerStyle=\"{StaticResource WardrobeCard.MoreMenuItem}\"", xaml);
    }

    [Fact]
    public void NavigationSidebar_ProfileMenuOpensPersonalCenter()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));

        Assert.Contains("个人中心", xaml);
        Assert.Contains("x:Name=\"ProfileHoverHint\"", xaml);
        Assert.Contains("RenderTransformOrigin=\"0.5,0.5\"", xaml);
        Assert.Contains("TransformGroup", xaml);
        Assert.Contains("Header = \"个人中心\"", code);
        Assert.Contains("new PersonalCenterDialog()", code);
        Assert.DoesNotContain("Header = \"编辑当前档案\"", code);
    }

    [Fact]
    public void PersonalCenterDialog_ExposesAccountProfileAndSecuritySections()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml.cs"));

        Assert.Contains("Text=\"个人中心\"", xaml);
        Assert.Contains("Content=\"账号资料\"", xaml);
        Assert.Contains("Content=\"个人档案\"", xaml);
        Assert.Contains("Content=\"安全\"", xaml);
        Assert.Contains("x:Name=\"AccountAvatar\"", xaml);
        Assert.Contains("Content=\"更换头像\"", xaml);
        Assert.Contains("Content=\"保存账号资料\"", xaml);
        Assert.Contains("Content=\"保存个人档案\"", xaml);
        Assert.Contains("Content=\"更新密码/PIN\"", xaml);
        Assert.Contains("UpdateOwnCredentialAsync", code);
        Assert.Contains("SaveAccount_Click", code);
        Assert.Contains("SaveProfile_Click", code);
        Assert.Contains("SaveSecurity_Click", code);
    }

    [Fact]
    public void AiImageSettings_OpenProfileEntryUsesPersonalCenter()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AiImageGenerationSettingsPanel.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AiImageGenerationSettingsPanel.xaml.cs"));

        Assert.Contains("Content=\"个人中心\"", xaml);
        Assert.Contains("new PersonalCenterDialog()", code);
        Assert.DoesNotContain("new PersonalProfileEditorPanel()", code);
        Assert.DoesNotContain("Content=\"编辑个人档案\"", xaml);
    }

    [Fact]
    public void PersonalCenterDialog_UpdatesAvatarAndNameFeedbackLive()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml.cs"));

        Assert.Contains("x:Name=\"TxtDisplayName\"", xaml);
        Assert.Contains("TextChanged=\"AccountIdentityChanged\"", xaml);
        Assert.Contains("RefreshAccountIdentityPreview()", code);
        Assert.Contains("ApplyAccountAvatarPreview()", code);
        Assert.Contains("BtnRemoveAccountAvatar", xaml);
        Assert.Contains("BtnRemoveProfileAvatar", xaml);
        Assert.Contains("BtnRemoveFullBody", xaml);
    }

    [Fact]
    public void PersonalCenterDialog_UsesWorkspaceStyleSections()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml"));

        Assert.Contains("x:Name=\"PersonalCenterTabBar\"", xaml);
        Assert.Contains("Orientation=\"Horizontal\"", xaml);
        Assert.Contains("x:Name=\"PersonalCenterTabRail\"", xaml);
        Assert.Contains("x:Name=\"AccountIdentityCard\"", xaml);
        Assert.Contains("x:Name=\"ProfileReferenceCard\"", xaml);
        Assert.Contains("x:Name=\"SecurityOverviewCard\"", xaml);
        Assert.Contains("Style=\"{StaticResource WorkbenchTextInput}\"", xaml);
        Assert.Contains("Style=\"{StaticResource WorkbenchPasswordInput}\"", xaml);
        Assert.Contains("x:Name=\"PromptPreviewCard\"", xaml);
        Assert.Contains("x:Name=\"ProfileReferenceHero\"", xaml);
        Assert.Contains("x:Name=\"ProfileAssistColumn\"", xaml);
        Assert.Contains("x:Name=\"ProfileReferenceActions\"", xaml);
        Assert.Contains("x:Name=\"ProfileFullBodyActions\"", xaml);
        Assert.Contains("x:Name=\"ProfileBasicsGrid\"", xaml);
        Assert.Contains("x:Key=\"PersonalCenter.TabRail\"", xaml);
        Assert.Contains("x:Key=\"PersonalCenter.ToolButton\"", xaml);
        Assert.Contains("x:Key=\"PersonalCenter.TabButton\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource AppSegmentedTabButton}\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource SecondaryButton}\"", xaml);
        Assert.Contains("<Setter Property=\"CornerRadius\" Value=\"16\"/>", xaml);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"34\"/>", xaml);
        Assert.Contains("x:Name=\"AccountSummaryHero\"", xaml);
        Assert.Contains("x:Name=\"ProfileProfileCard\"", xaml);
        Assert.Contains("HorizontalAlignment=\"Left\"", xaml);
        Assert.Contains("Margin=\"0,2,0,0\"", xaml);
        Assert.DoesNotContain("当前用户工作台", xaml);
        Assert.DoesNotContain("集中维护账号资料、效果图参考档案和登录安全。", xaml);
        Assert.Contains("x:Name=\"AvatarPreview\"", xaml);
        Assert.Contains("x:Name=\"FullBodyPreview\"", xaml);
        Assert.Contains("Stretch=\"Uniform\"", xaml);
        Assert.Contains("Style=\"{StaticResource SettingsGhostButton}\"", xaml);
        Assert.DoesNotContain("Style=\"{StaticResource SecondaryButton}\"", xaml);
        Assert.DoesNotContain("x:Name=\"AvatarPreview\"\r\n                                                               Stretch=\"UniformToFill\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_DoesNotOfferSessionSwitching()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs"));

        Assert.DoesNotContain("重新登录", xaml);
        Assert.DoesNotContain("SwitchUser_Click", xaml);
        Assert.DoesNotContain("SwitchUser_Click", code);
        Assert.DoesNotContain("CanSwitch", code);
    }

    [Fact]
    public void OutfitWorkspaceDialog_UsesSharedActionButtons()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/OutfitWorkspaceDialog.xaml"));

        Assert.Contains("x:Key=\"Workspace.ActionButton\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource SecondaryButton}\"", xaml);
        Assert.Contains("x:Key=\"Workspace.PrimaryButton\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource ModalSaveButton}\"", xaml);
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
