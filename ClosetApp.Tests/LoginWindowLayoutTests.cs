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
    public void LoginWindow_UsesCompactFloatingFormLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"LoginErrorHost\"", xaml);
        Assert.Contains("x:Key=\"LoginSubmitButton\"", xaml);
        Assert.Contains("x:Key=\"LoginTextContextMenu\"", xaml);
        Assert.Contains("x:Key=\"LoginPasswordContextMenu\"", xaml);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"56\"/>", xaml);
        Assert.Contains("x:Name=\"LoginThemeToggle\"", xaml);
        Assert.Contains("HorizontalAlignment=\"Right\"", xaml);
        Assert.Contains("Panel.ZIndex=\"2\"", xaml);
        Assert.DoesNotContain("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.DoesNotContain("Effect=\"{StaticResource LoginCardShadow}\"", xaml);
    }

    [Fact]
    public void LoginWindow_DefaultSizeLeavesRoomForLoginForm()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("Width=\"1040\"", xaml);
        Assert.Contains("Height=\"780\"", xaml);
        Assert.Contains("MinWidth=\"1040\"", xaml);
        Assert.Contains("MinHeight=\"780\"", xaml);
        Assert.Contains("MaxWidth=\"1040\"", xaml);
        Assert.Contains("MaxHeight=\"780\"", xaml);
        Assert.Contains("ResizeMode=\"NoResize\"", xaml);
        Assert.Contains("Background=\"{DynamicResource SurfacePageBrush}\"", xaml);
        Assert.Contains("x:Name=\"LoginWorkspaceShell\"", xaml);
        Assert.Contains("HorizontalAlignment=\"Center\"", xaml);
        Assert.DoesNotContain("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.DoesNotContain("Effect=\"{StaticResource LoginCardShadow}\"", xaml);
    }

    [Fact]
    public void LoginWindow_UsesCenteredCardLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"LoginWorkspaceShell\"", xaml);
        Assert.Contains("x:Name=\"LoginHeroSurface\"", xaml);
        Assert.Contains("x:Name=\"LoginFormSurface\"", xaml);
        Assert.Contains("x:Name=\"LoginActionRail\"", xaml);
        Assert.Contains("x:Name=\"HeroRecentAccountBlock\"", xaml);
        Assert.Contains("Style=\"{StaticResource LoginRecentAccountButton}\"", xaml);
        Assert.Contains("x:Name=\"FirstTimeHintBlock\"", xaml);
        Assert.DoesNotContain("x:Name=\"HeroVisualStage\"", xaml);
        Assert.DoesNotContain("x:Name=\"HeroSignalsGrid\"", xaml);
        Assert.DoesNotContain("<ScrollViewer VerticalScrollBarVisibility=\"Auto\"", xaml);
        Assert.DoesNotContain("CornerRadius=\"24\"", xaml);
    }

    [Fact]
    public void LoginWindow_RendersRecentAccountDropdown()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"RecentAccountSelector\"", xaml);
        Assert.Contains("CornerRadius=\"16\"", xaml);
        Assert.Contains("<Setter Property=\"Height\" Value=\"52\"/>", xaml);
        Assert.Contains("IsEditable=\"True\"", xaml);
        Assert.Contains("TextSearch.TextPath=\"AccountName\"", xaml);
        Assert.Contains("Text=\"账号\"", xaml);
        Assert.Contains("Text=\"密码\"", xaml);
        Assert.Contains("x:Name=\"TxtSubmitLabel\"", xaml);
        Assert.Contains("x:Name=\"SubmitBusyIndicator\"", xaml);
        Assert.Contains("Text=\"我的衣橱\"", xaml);
        Assert.Contains("Text=\"Closet Companion&#x0a;记录搭配与衣柜管理\"", xaml);
        Assert.Contains("x:Name=\"HeroRecentAccountBlock\"", xaml);
        Assert.Contains("x:Name=\"TxtRecentAccountName\"", xaml);
        Assert.Contains("x:Name=\"TxtRecentAccountLastLogin\"", xaml);
        Assert.Contains("Text=\"最近使用\"", xaml);
        Assert.Contains("Text=\"填入\"", xaml);
        Assert.DoesNotContain("Width=\"250\"", xaml);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
        Assert.Contains("ContextMenu=\"{StaticResource LoginTextContextMenu}\"", xaml);
        Assert.Contains("<Setter Property=\"ContextMenu\" Value=\"{StaticResource LoginPasswordContextMenu}\"/>", xaml);
        Assert.Contains("x:Name=\"MultiUserModeInfoPanel\"", xaml);
        Assert.Contains("Style=\"{StaticResource LoginInlineLinkButton}\"", xaml);
        Assert.Contains("x:Name=\"LoginErrorIcon\"", xaml);
        Assert.Contains("x:Name=\"TxtErrorTitle\"", xaml);
        Assert.Contains("Background=\"{DynamicResource DangerLightBrush}\"", xaml);
        Assert.DoesNotContain("ContentStringFormat=\"{Binding SelectionBoxItemStringFormat", xaml);
        Assert.DoesNotContain("SelectionBoxItemStringFormat", xaml);
        Assert.DoesNotContain("x:Name=\"LoginAccountBox\"", xaml);
        Assert.DoesNotContain("x:Name=\"RecentAccountsPanel\"", xaml);
        Assert.DoesNotContain("x:Name=\"RecentAccountsHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"SelectedAccountCard\"", xaml);
        Assert.DoesNotContain("x:Name=\"WelcomeInsightStack\"", xaml);
        Assert.DoesNotContain("x:Name=\"WelcomeAccountIdentityCard\"", xaml);
        Assert.DoesNotContain("x:Name=\"WelcomeSessionCard\"", xaml);
        Assert.DoesNotContain("Text=\"账号工作区\"", xaml);
        Assert.DoesNotContain("Text=\"LOCAL WORKSPACE\"", xaml);
        Assert.DoesNotContain("Text=\"独立保存\"", xaml);
    }

    [Fact]
    public void LoginWindow_UsesInlineMultiUserExplanation()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml.cs"));

        Assert.Contains("MultiUserModeInfoPanel.Visibility", code);
        Assert.Contains("passwordBox.Clear();", code);
        Assert.DoesNotContain("ConfirmModal.ShowMessageAsync", code);
        Assert.DoesNotContain("ModalService.Instance.Show", code);
        Assert.DoesNotContain("这是一个本地多用户工作区。", xaml);
        Assert.Contains("登录页本身不开放公开注册", xaml);
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
    public void LoginWindow_DropsSplitShowcaseContent()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.DoesNotContain("Text=\"记录灵感\"", xaml);
        Assert.DoesNotContain("Text=\"整理有序\"", xaml);
        Assert.DoesNotContain("Text=\"独立保存\"", xaml);
        Assert.DoesNotContain("x:Name=\"HeroVisualStage\"", xaml);
        Assert.DoesNotContain("x:Name=\"HeroSignalsGrid\"", xaml);
        Assert.DoesNotContain("CornerRadius=\"24\"", xaml);
    }

    [Fact]
    public void UserManagementDialog_CodeBehindSupportsSessionBarLayout()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml.cs"));

        Assert.Contains("ApplyCurrentUserHero", code);
        Assert.Contains("CurrentSessionAvatar.AvatarPath", code);
        Assert.Contains("TxtCurrentSessionUser.Text", code);
        Assert.Contains("TxtCurrentSessionContext.Text", code);
        Assert.DoesNotContain("CurrentUserHeroAvatar.AvatarPath", code);
    }

    [Fact]
    public void UserManagementDialog_UsesSelectedUserWorkbenchLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/LocalUserManagementDialog.xaml"));

        Assert.Contains("x:Name=\"CurrentSessionBar\"", xaml);
        Assert.Contains("x:Name=\"CurrentSessionAvatar\"", xaml);
        Assert.Contains("x:Name=\"TxtCurrentSessionUser\"", xaml);
        Assert.Contains("x:Name=\"TxtCurrentSessionContext\"", xaml);
        Assert.Contains("x:Name=\"MemberManagementCard\"", xaml);
        Assert.Contains("x:Name=\"MemberSectionHeader\"", xaml);
        Assert.Contains("x:Name=\"MemberListActionBar\"", xaml);
        Assert.Contains("x:Name=\"TxtUserSearch\"", xaml);
        Assert.Contains("x:Name=\"BtnCreateUserInline\"", xaml);
        Assert.Contains("x:Name=\"CreateUserPanel\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserWorkbench\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserHeroCard\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserAccountSection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserSecuritySection\"", xaml);
        Assert.Contains("x:Name=\"SelectedUserDangerSection\"", xaml);
        Assert.Contains("Content=\"删除用户\"", xaml);
        Assert.Contains("Content=\"保存资料\"", xaml);
        Assert.Contains("Content=\"重置凭证\"", xaml);
        Assert.DoesNotContain("x:Name=\"UserManagerStatsBar\"", xaml);
        Assert.DoesNotContain("x:Name=\"UserManagerToolbar\"", xaml);
        Assert.DoesNotContain("x:Name=\"UserManagerWorkspaceCard\"", xaml);
        Assert.DoesNotContain("x:Name=\"BtnShowCreateUser\"", xaml);
        Assert.DoesNotContain("x:Name=\"CurrentUserWorkbenchCard\"", xaml);
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
        Assert.Contains("x:Name=\"ProfileChevronShell\"", xaml);
        Assert.Contains("x:Name=\"TxtClothingCount\"", xaml);
        Assert.Contains("ToolTip=\"个人中心 / 账号菜单\"", xaml);
        Assert.Contains("RenderTransformOrigin=\"0.5,0.5\"", xaml);
        Assert.Contains("TransformGroup", xaml);
        Assert.Contains("Header = \"个人中心\"", code);
        Assert.Contains("ShowCached<PersonalCenterDialog>()", code);
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
        Assert.Contains("ShowCached<PersonalCenterDialog>()", code);
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
    public void PersonalCenterDialog_SavingAccountOnlyRefreshesShellUser()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml.cs"));
        var mainWindowCode = File.ReadAllText(FindProjectFile("ClosetApp.UI/MainWindow.xaml.cs"));

        Assert.Contains("RefreshCurrentUserShellAsync", code);
        Assert.Contains("RefreshCurrentUserShellAsync", mainWindowCode);
        Assert.DoesNotContain("SetCurrentUserIdAsync(_currentUserId)", code);
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
        Assert.Contains("x:Name=\"AccountHeroIdentityStack\"", xaml);
        Assert.Contains("x:Name=\"AccountHeroMetricsRow\"", xaml);
        Assert.Contains("x:Name=\"AccountHeroActionRow\"", xaml);
        Assert.Contains("x:Name=\"AccountAvatarPreview\"", xaml);
        Assert.Contains("x:Name=\"AccountHeroStatusRail\"", xaml);
        Assert.Contains("x:Name=\"AccountHeroTouchHint\"", xaml);
        Assert.Contains("Columns=\"2\"", xaml);
        Assert.Contains("x:Name=\"AccountWorkspaceGrid\"", xaml);
        Assert.Contains("<ColumnDefinition Width=\"280\"/>", xaml);
        Assert.Contains("Width=\"156\"", xaml);
        Assert.Contains("Height=\"156\"", xaml);
        Assert.Contains("Text=\"当前账号\"", xaml);
        Assert.Contains("Text=\"上传后这里会立即预览，保存后才会正式替换账号头像。\"", xaml);
        Assert.Contains("Text=\"侧边栏、菜单与登录页都会复用这张头像。\"", xaml);
        Assert.Contains("Grid.Row=\"6\"", xaml);
        Assert.Contains("x:Name=\"ProfileProfileCard\"", xaml);
        Assert.Contains("HorizontalAlignment=\"Left\"", xaml);
        Assert.Contains("Margin=\"0,10,0,0\"", xaml);
        Assert.DoesNotContain("当前用户工作台", xaml);
        Assert.DoesNotContain("集中维护账号资料、效果图参考档案和登录安全。", xaml);
        Assert.DoesNotContain("Text=\"{Binding ThemeSummary}\"", xaml);
        Assert.Contains("x:Name=\"AvatarPreview\"", xaml);
        Assert.Contains("x:Name=\"FullBodyPreview\"", xaml);
        Assert.Contains("Stretch=\"Uniform\"", xaml);
        Assert.Contains("Style=\"{StaticResource SettingsGhostButton}\"", xaml);
        Assert.DoesNotContain("Style=\"{StaticResource SecondaryButton}\"", xaml);
        Assert.DoesNotContain("x:Name=\"AvatarPreview\"\r\n                                                               Stretch=\"UniformToFill\"", xaml);
        Assert.DoesNotContain("Grid.Row=\"2\"\r\n                                        x:Name=\"AccountIdentityCard\"", xaml);
    }

    [Fact]
    public void PersonalCenterDialog_GuardsSectionControlsDuringInitialization()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/PersonalCenterDialog.xaml.cs"));

        Assert.Contains("if (AccountPanel != null)", code);
        Assert.Contains("if (ProfilePanel != null)", code);
        Assert.Contains("if (SecurityPanel != null)", code);
        Assert.Contains("if (BtnAccountTab != null)", code);
        Assert.Contains("if (BtnProfileTab != null)", code);
        Assert.Contains("if (BtnSecurityTab != null)", code);
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
    public void NavigationSidebar_UsesCachedModalOpenPathForHeavyDialogs()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml.cs"));
        var modalServiceCode = File.ReadAllText(FindProjectFile("ClosetApp.UI/Services/ModalService.cs"));

        Assert.Contains("ShowCached<PersonalCenterDialog>()", code);
        Assert.Contains("ShowCached<LocalUserManagementDialog>()", code);
        Assert.Contains("DispatcherPriority.Loaded", modalServiceCode);
        Assert.Contains("GetOrCreateCachedView", modalServiceCode);
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

    [Fact]
    public void ModalCloseButton_DoesNotSetStyleFromInsideItself()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/ModalCardStyles.xaml"));

        Assert.Contains("x:Key=\"ModalCloseButton\"", xaml);
        Assert.Contains("BasedOn=\"{StaticResource IconButton}\"", xaml);
        Assert.DoesNotContain("<Setter Property=\"Style\" Value=\"{StaticResource IconButton}\"/>", xaml);
    }

    [Fact]
    public void LoginWindow_ContainsThemeToggle()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"LoginThemeToggle\"", xaml);
        Assert.Contains("x:Name=\"BtnThemeRose\"", xaml);
        Assert.Contains("x:Name=\"BtnThemeBlue\"", xaml);
        Assert.Contains("Content=\"柔粉\"", xaml);
        Assert.Contains("Content=\"清蓝\"", xaml);
        Assert.Contains("Style=\"{StaticResource AppSegmentedTabShell}\"", xaml);
    }

    [Fact]
    public void LoginWindow_ContainsFirstTimeHint()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.Contains("x:Name=\"FirstTimeHintBlock\"", xaml);
        Assert.Contains("首次使用", xaml);
        Assert.Contains("管理员初始化", xaml);
        Assert.Contains("了解本地多用户模式", xaml);
        Assert.DoesNotContain("Content=\"注册\"", xaml);
        Assert.DoesNotContain("Content=\"创建账号\"", xaml);
        Assert.DoesNotContain("Content=\"新增成员\"", xaml);
    }

    [Fact]
    public void LoginWindow_DoesNotContainPinControls()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml"));

        Assert.DoesNotContain("x:Name=\"BtnPinMode\"", xaml);
        Assert.DoesNotContain("x:Name=\"PinCredentialPanel\"", xaml);
        Assert.DoesNotContain("x:Name=\"SetupPinBox\"", xaml);
        Assert.DoesNotContain("x:Name=\"LoginPinBox\"", xaml);
        Assert.DoesNotContain("快捷 PIN", xaml);
        Assert.DoesNotContain("x:Name=\"CredentialModePanel\"", xaml);
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
