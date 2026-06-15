using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsLayoutTests
{
    [Fact]
    public void SettingsTab_UsesSectionBasedSingleFlowLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"SettingsSectionFlow\"", xaml);
        Assert.Contains("x:Name=\"SettingsLocationWeatherSection\"", xaml);
        Assert.Contains("x:Name=\"SettingsStorageSection\"", xaml);
        Assert.Contains("x:Name=\"SettingsSystemSection\"", xaml);
        Assert.Contains("Text=\"外观\"", xaml);
        Assert.Contains("Text=\"位置与天气\"", xaml);
        Assert.Contains("Text=\"存储\"", xaml);
        Assert.Contains("Text=\"系统\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewStrip\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewPrimaryGrid\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsGroupDivider\"", xaml);
        Assert.DoesNotContain("Text=\"设置概览\"", xaml);
        Assert.DoesNotContain("Text=\"天气与城市\"", xaml);
        Assert.DoesNotContain("Text=\"文件位置\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsWorkbenchColumns\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsDailyWorkbench\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsMaintenanceWorkbench\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewHero\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricMatrix\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewCompactSummaryGrid\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewMetricsGrid\"", xaml);
        Assert.DoesNotContain("设置工作台", xaml);
        Assert.DoesNotContain("当前状态", xaml);
        Assert.DoesNotContain("常用偏好、图片资产和备份维护都集中在这里。", xaml);
        Assert.DoesNotContain("SettingsWorkbenchBadge", xaml);
        Assert.DoesNotContain("SettingsWorkbenchColumnBadge", xaml);
        Assert.DoesNotContain("SettingsWorkbenchColumnBadgeText", xaml);
        Assert.DoesNotContain("Text=\"日常使用\"", xaml);
        Assert.DoesNotContain("Text=\"存储与治理\"", xaml);
    }

    [Fact]
    public void SettingsTab_UsesTactileOverviewSurfaces()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml.cs"));

        Assert.Contains("BringSectionIntoView", code);
        Assert.Contains("AppearancePanel.ApplyFontPresetSelection(_viewModel.FontSizePreset);", code);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewHeroSurface\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsWorkbenchSectionDivider\"", xaml);
        Assert.DoesNotContain("x:Name=\"SettingsOverviewSignalRail\"", xaml);
        Assert.DoesNotContain("OverviewThemeShortcut_Click", code);
        Assert.DoesNotContain("OverviewAiShortcut_Click", code);
        Assert.DoesNotContain("ApplyFontSizeSelection", code);
        Assert.DoesNotContain("ScrollToAiSection", code);
    }

    [Fact]
    public void AppearanceSettingsPanel_UsesSingleCardWorkbenchLayoutWithoutAppInfo()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml"));

        Assert.Contains("x:Name=\"AppearanceFlowStack\"", xaml);
        Assert.Contains("x:Name=\"AppearanceWorkbenchCard\"", xaml);
        Assert.Contains("x:Name=\"AppearanceThemeSection\"", xaml);
        Assert.Contains("x:Name=\"AppearanceFontSizeSection\"", xaml);
        Assert.Contains("x:Name=\"AppearanceDisplayModeSection\"", xaml);
        Assert.Contains("x:Name=\"AppearanceSectionDivider\"", xaml);
        Assert.Contains("x:Name=\"AppearanceThemeCompactList\"", xaml);
        Assert.Contains("Style=\"{StaticResource SettingsInsetCard}\"", xaml);
        Assert.Contains("Text=\"主题\"", xaml);
        Assert.Contains("Text=\"字体大小\"", xaml);
        Assert.Contains("Text=\"默认展示\"", xaml);
        Assert.Contains("Orientation=\"Horizontal\"", xaml);
        Assert.Contains("AppSegmentedTabShell", xaml);
        Assert.Contains("x:Name=\"RadioFontCompact\"", xaml);
        Assert.Contains("x:Name=\"RadioFontBalanced\"", xaml);
        Assert.Contains("x:Name=\"RadioFontExpanded\"", xaml);
        Assert.Contains("FontSizeLevel_Checked", xaml);
        Assert.DoesNotContain("x:Name=\"AppearanceAppSection\"", xaml);
        Assert.DoesNotContain("Text=\"应用\"", xaml);
        Assert.DoesNotContain("把主题、字体和默认展示集中在一张工作卡里。", xaml);
        Assert.DoesNotContain("保留柔和氛围，切换实时生效。", xaml);
        Assert.DoesNotContain("三档足够，优先读起来舒服。", xaml);
        Assert.DoesNotContain("决定首页先看搭配结构还是效果图。", xaml);
        Assert.DoesNotContain("x:Name=\"AppearancePreferenceSummaryCard\"", xaml);
        Assert.DoesNotContain("选择整体配色，并设置搭配卡片默认展示方式。", xaml);
        Assert.DoesNotContain("设置列表默认更偏向搭配预览，还是效果图主视觉。", xaml);
        Assert.DoesNotContain("当前界面主题与运行环境摘要。", xaml);
        Assert.DoesNotContain("x:Name=\"AppearancePreviewSummaryCard\"", xaml);
        Assert.DoesNotContain("SettingsEyebrowBadge", xaml);
        Assert.DoesNotContain("SettingsEyebrowBadgeText", xaml);
    }

    [Fact]
    public void AppearanceSettingsPanel_DoesNotPersistProgrammaticSelectionRefresh()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml.cs"));

        Assert.Contains("_isApplyingSelection", code);
        Assert.Contains("ApplySelectionSilently", code);
        Assert.Contains("if (_isApplyingSelection)", code);
        Assert.Contains("RadioOutfitFirst.IsChecked = mode == OutfitCardDisplayMode.OutfitFirst;", code);
        Assert.Contains("ApplyFontPresetSelection", code);
        Assert.Contains("MapFontPresetToLevel", code);
    }

    [Fact]
    public void AppearanceSettingsPanel_UsesParentDataContextForRefreshState()
    {
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml.cs"));

        Assert.DoesNotContain("App.Services.GetRequiredService<SettingsViewModel>()", code);
        Assert.Contains("if (DataContext is SettingsViewModel viewModel)", code);
        Assert.Contains("ApplyOutfitCardDisplaySelection(viewModel.DefaultOutfitCardDisplayMode);", code);
        Assert.Contains("ApplyFontPresetSelection(viewModel.FontSizePreset);", code);
    }

    [Fact]
    public void ThemeCard_UsesCompactThemeSelectorLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml.cs"));

        Assert.Contains("x:Name=\"CardRootButton\"", xaml);
        Assert.Contains("RenderTransformOrigin=\"0.5,0.5\"", xaml);
        Assert.Contains("x:Name=\"ThemeChoiceDot\"", xaml);
        Assert.Contains("x:Name=\"ThemeSwatchRow\"", xaml);
        Assert.Contains("x:Name=\"ActionPill\"", xaml);
        Assert.Contains("MinWidth=\"120\"", xaml);
        Assert.Contains("MinHeight=\"72\"", xaml);
        Assert.Contains("x:Name=\"ThemePreviewField\"", xaml);
        Assert.Contains("x:Name=\"ThemeFooterRow\"", xaml);
        Assert.Contains("Width=\"10\"", xaml);
        Assert.Contains("Height=\"10\"", xaml);
        Assert.Contains("x:Name=\"StateHint\"", xaml);
        Assert.DoesNotContain("x:Name=\"BtnSelect\"", xaml);
        Assert.DoesNotContain("x:Name=\"PreviewHost\"", xaml);
        Assert.DoesNotContain("x:Name=\"SelectedBadge\"", xaml);
        Assert.Contains("CardRootButton.IsEnabled = !IsSelected;", code);
        Assert.Contains("ActionPill.BorderBrush", code);
        Assert.Contains("ThemeChoiceDot.BorderBrush", code);
        Assert.Contains("StateHint.Text", code);
    }

    [Fact]
    public void WeatherAiBackupPanels_UseCompactWorkbenchSections()
    {
        var weatherXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml"));
        var aiXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AiImageGenerationSettingsPanel.xaml"));
        var backupXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/BackupSettingsPanel.xaml"));

        Assert.Contains("x:Name=\"WeatherSummaryCard\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherPreferenceCard\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherPreferenceSection\"", weatherXaml);
        Assert.DoesNotContain("把默认城市、天气和今日推荐偏好收在一处。", weatherXaml);
        Assert.DoesNotContain("城市输入、实时天气和刷新动作都集中在这里。", weatherXaml);
        Assert.DoesNotContain("场景、轮换策略和避开今日已穿可以一起调整。", weatherXaml);
        Assert.DoesNotContain("后续天气穿搭推荐也会直接复用这里。", weatherXaml);
        Assert.DoesNotContain("设置默认城市，并调整今日推荐偏好。", weatherXaml);
        Assert.DoesNotContain("影响搭配页的天气推荐结果。", weatherXaml);
        Assert.DoesNotContain("SettingsEyebrowBadge", weatherXaml);

        Assert.Contains("x:Name=\"AiConnectionGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCredentialCard\"", aiXaml);
        Assert.Contains("x:Name=\"AiModelGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCredentialActions\"", aiXaml);
        Assert.Contains("x:Name=\"AiStatusCard\"", aiXaml);
        Assert.Contains("x:Name=\"AiCustomModelPanel\"", aiXaml);
        Assert.Contains("x:Name=\"AiConnectionMetaGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCredentialActionRow\"", aiXaml);
        Assert.DoesNotContain("<ControlTemplate TargetType=\"Button\">", aiXaml);
        Assert.Contains("VerticalAlignment=\"Stretch\"", aiXaml);
        Assert.Contains("x:Name=\"AiConnectionContent\"", aiXaml);
        Assert.Contains("Width=\"170\"", aiXaml);
        Assert.Contains("Content=\"测试连接\"", aiXaml);
        Assert.DoesNotContain("x:Name=\"AiPresetCard\"", aiXaml);
        Assert.DoesNotContain("x:Name=\"AiPresetGrid\"", aiXaml);
        Assert.DoesNotContain("快捷预设", aiXaml);
        Assert.DoesNotContain("gpt-image-2 · 主路线", aiXaml);
        Assert.DoesNotContain("gpt-image-1.5 · 高质量", aiXaml);
        Assert.DoesNotContain("当前中转主路线", aiXaml);
        Assert.DoesNotContain("更高质量优先", aiXaml);
        Assert.DoesNotContain("让生成配置更稳定也更容易切换。", aiXaml);
        Assert.DoesNotContain("凭证与说明", aiXaml);
        Assert.DoesNotContain("管理接口、模型和 API Key。", aiXaml);
        Assert.DoesNotContain("SettingsEyebrowBadge", aiXaml);

        Assert.Contains("x:Name=\"BackupActionGrid\"", backupXaml);
        Assert.Contains("x:Name=\"BackupExportCard\"", backupXaml);
        Assert.Contains("x:Name=\"BackupImportCard\"", backupXaml);
        Assert.Contains("x:Name=\"BackupHistoryCard\"", backupXaml);
        Assert.Contains("x:Name=\"BackupExportActions\"", backupXaml);
        Assert.Contains("x:Name=\"BackupHistoryActions\"", backupXaml);
        Assert.Contains("BtnQuickExportBackup", backupXaml);
        Assert.Contains("Style=\"{StaticResource SettingsGhostButton}\"", backupXaml);
        Assert.Contains("Columns=\"2\"", backupXaml);
        Assert.Contains("Style=\"{StaticResource SettingsDangerGhostButton}\"", backupXaml);
        Assert.DoesNotContain("导出、导入和历史记录。", backupXaml);
        Assert.DoesNotContain("导出、导入与历史。", backupXaml);
        Assert.DoesNotContain("SettingsEyebrowBadge", backupXaml);
    }

    [Fact]
    public void SettingsStyles_UseUnifiedButtonsAndInputs()
    {
        var settingsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Settings.xaml"));
        var radiusXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Tokens/Radius.xaml"));
        var motionXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Tokens/Motion.xaml"));
        var buttonsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Buttons.xaml"));
        var inputsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Inputs.xaml"));
        var typographyXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Tokens/Typography.xaml"));
        var weatherXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml"));
        var aiXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AiImageGenerationSettingsPanel.xaml"));
        var imageXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/ImageMaintenanceSettingsPanel.xaml"));
        var logXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/LogMaintenanceSettingsPanel.xaml"));

        Assert.Contains("x:Key=\"Radius.XLarge\"", radiusXaml);
        Assert.Contains("x:Key=\"Radius.Control\"", radiusXaml);
        Assert.Contains("<CornerRadius x:Key=\"Radius.XLarge\">", radiusXaml);
        Assert.Contains("<CornerRadius x:Key=\"Radius.Control\">", radiusXaml);
        Assert.Contains("x:Key=\"Motion.PressMs\"", motionXaml);
        Assert.Contains("x:Key=\"Motion.ModalMs\"", motionXaml);
        Assert.Contains("x:Key=\"HoverLiftUp\"", buttonsXaml);
        Assert.Contains("Storyboard.TargetName=\"BtnBorder\"", buttonsXaml);
        Assert.Contains("TranslateTransform", buttonsXaml);
        Assert.Contains("x:Key=\"InputFocusGlow\"", inputsXaml);
        Assert.Contains("x:Key=\"ComboBoxPopupLift\"", inputsXaml);
        Assert.Contains("x:Key=\"SettingsDangerButton\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsFieldInput\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsFieldComboBox\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsEditableComboBox\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsCardHeaderGrid\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsCardTitleText\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsCardSubtitleText\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsCompactSectionCard\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsWorkspaceGrid\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsRowFieldLabelText\"", settingsXaml);
        Assert.Contains("x:Key=\"SettingsInlineHintText\"", settingsXaml);
        Assert.Contains("x:Key=\"FontSize.PageTitle\"", typographyXaml);
        Assert.Contains("x:Key=\"FontSize.SectionTitle\"", typographyXaml);
        Assert.Contains("x:Key=\"FontSize.Body\"", typographyXaml);
        Assert.Contains("x:Key=\"FontSize.Tiny\"", typographyXaml);
        Assert.Contains("Value=\"{DynamicResource FontSize.Label}\"", settingsXaml);
        Assert.Contains("Value=\"{DynamicResource Button.FontSize.Medium}\"", buttonsXaml);
        Assert.Contains("Value=\"{DynamicResource FontSize.Input}\"", inputsXaml);
        Assert.Contains("Value=\"{DynamicResource FontSize.Hint}\"", inputsXaml);
        Assert.Contains("Value=\"{DynamicResource FontSize.Meta}\"", inputsXaml);
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Body}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Hero}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/OutfitsTab.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Body}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/NavigationSidebar.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Hero}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/LoginWindow.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Body}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/ClothesTab.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Body}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/TagsTab.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Hint}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/SearchBox.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Meta}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.PageTitle}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/Modal/ConfirmDialog.xaml")));
        Assert.Contains("FontSize=\"{DynamicResource FontSize.Hint}\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/PremiumClothingCard.xaml")));
        Assert.Contains("Text=\"{Binding Text, RelativeSource={RelativeSource TemplatedParent}, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"", settingsXaml);
        Assert.Contains("Cursor=\"IBeam\"", settingsXaml);
        Assert.Contains("Grid.Column=\"1\"", settingsXaml);
        Assert.Contains("Style=\"{StaticResource SettingsEditableComboBox}\"", weatherXaml);
        Assert.DoesNotContain("Style=\"{StaticResource SettingsFieldInput}\"", weatherXaml);
        Assert.Contains("Style=\"{StaticResource SettingsFieldComboBox}\"", aiXaml);
        Assert.Contains("Style=\"{StaticResource SettingsDangerButton}\"", imageXaml);
        Assert.Contains("Style=\"{StaticResource SettingsDangerGhostButton}\"", logXaml);
    }

    [Fact]
    public void StorageImageLogPanels_ShareCompactSectionRhythm()
    {
        var storageXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/StorageLocationsSettingsPanel.xaml"));
        var imageXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/ImageMaintenanceSettingsPanel.xaml"));
        var logXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/LogMaintenanceSettingsPanel.xaml"));
        var weatherXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml"));

        Assert.Contains("x:Name=\"StorageHeaderGrid\"", storageXaml);
        Assert.Contains("x:Name=\"StorageWorkbenchCard\"", storageXaml);
        Assert.Contains("x:Name=\"StoragePathsStack\"", storageXaml);
        Assert.Contains("x:Name=\"StorageActionRail\"", storageXaml);
        Assert.Contains("x:Name=\"StorageRowGrid\"", storageXaml);
        Assert.Contains("Text=\"数据库\"", storageXaml);
        Assert.Contains("Text=\"目录与路径\"", storageXaml);
        Assert.Contains("Content=\"打开日志\"", storageXaml);
        Assert.DoesNotContain("常用目录、数据库和缓存都集中在这张工作卡里。", storageXaml);
        Assert.DoesNotContain("遇到图片、数据库或缓存问题时能快速定位。", storageXaml);
        Assert.DoesNotContain("常用目录与数据位置。", storageXaml);
        Assert.DoesNotContain("Style=\"{StaticResource SettingsMicroCard}\"", storageXaml);

        Assert.Contains("x:Name=\"ImageStatsGrid\"", imageXaml);
        Assert.Contains("x:Name=\"ImageHealthGrid\"", imageXaml);
        Assert.Contains("x:Name=\"ImageActionsWrap\"", imageXaml);
        Assert.Contains("x:Name=\"ImagePrimaryActions\"", imageXaml);
        Assert.Contains("x:Name=\"ImageDangerActions\"", imageXaml);
        Assert.Contains("Text=\"当前概况\"", imageXaml);
        Assert.Contains("Text=\"图片治理\"", imageXaml);
        Assert.Contains("x:Key=\"SettingsDangerGhostButton\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Settings.xaml")));
        Assert.Contains("Style=\"{StaticResource SettingsDangerGhostButton}\"", imageXaml);
        Assert.DoesNotContain("缓存、缺图和孤儿原图。", imageXaml);
        Assert.DoesNotContain("SettingsEyebrowBadge", imageXaml);

        Assert.Contains("x:Name=\"LogHeaderGrid\"", logXaml);
        Assert.Contains("x:Name=\"LogSummaryCard\"", logXaml);
        Assert.Contains("Text=\"日志目录\"", logXaml);
        Assert.Contains("Text=\"日志维护\"", logXaml);
        Assert.Contains("Style=\"{StaticResource SettingsDangerGhostButton}\"", logXaml);
        Assert.DoesNotContain("查看目录并清理历史日志。", logXaml);
        Assert.DoesNotContain("查看日志位置与清理状态。", logXaml);
        Assert.DoesNotContain("SettingsEyebrowBadge", logXaml);

        Assert.Contains("x:Name=\"WeatherWorkbenchGrid\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherActionRow\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherContentStack\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherPreferenceGrid\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherStatusRail\"", weatherXaml);
        Assert.Contains("x:Name=\"RecommendationStatusRail\"", weatherXaml);
        Assert.Contains("Text=\"天气概况\"", weatherXaml);
        Assert.Contains("Style=\"{StaticResource SettingsCompactSectionCard}\"", weatherXaml);
        Assert.Contains("Style=\"{StaticResource SettingsInsetCard}\"", weatherXaml);
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
