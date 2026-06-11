using System.IO;
using Xunit;

namespace ClosetApp.Tests;

public class SettingsLayoutTests
{
    [Fact]
    public void SettingsTab_UsesWorkbenchOverviewStructure()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Views/SettingsTab.xaml"));

        Assert.Contains("x:Name=\"SettingsOverviewHero\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewPrimaryGrid\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewMetricsGrid\"", xaml);
        Assert.Contains("x:Name=\"SettingsOverviewQuickFactsPanel\"", xaml);
        Assert.Contains("x:Name=\"SettingsWorkbenchColumns\"", xaml);
        Assert.Contains("x:Name=\"SettingsDailyWorkbench\"", xaml);
        Assert.Contains("x:Name=\"SettingsMaintenanceWorkbench\"", xaml);
        Assert.DoesNotContain("设置工作台", xaml);
        Assert.DoesNotContain("当前状态", xaml);
        Assert.DoesNotContain("常用偏好、图片资产和备份维护都集中在这里。", xaml);
    }

    [Fact]
    public void AppearanceSettingsPanel_UsesWorkbenchCards()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AppearanceSettingsPanel.xaml"));

        Assert.Contains("x:Name=\"AppearanceWorkbenchHeader\"", xaml);
        Assert.Contains("x:Name=\"AppearanceDisplayModeCard\"", xaml);
        Assert.Contains("x:Name=\"AppearanceAppInfoCard\"", xaml);
        Assert.Contains("x:Name=\"AppearanceThemeGrid\"", xaml);
        Assert.Contains("VerticalAlignment=\"Top\"", xaml);
        Assert.Contains("AppSegmentedTabShell", xaml);
    }

    [Fact]
    public void ThemeCard_UsesCompactThemeSampleLayout()
    {
        var xaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml"));
        var code = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Shared/ThemeCard.xaml.cs"));

        Assert.Contains("x:Name=\"CardRootButton\"", xaml);
        Assert.Contains("RenderTransformOrigin=\"0.5,0.5\"", xaml);
        Assert.Contains("x:Name=\"PreviewToneRail\"", xaml);
        Assert.Contains("x:Name=\"ActionPill\"", xaml);
        Assert.Contains("MinHeight=\"172\"", xaml);
        Assert.Contains("x:Name=\"TonePreviewPanel\"", xaml);
        Assert.Contains("x:Name=\"PreviewHost\"", xaml);
        Assert.Contains("MinHeight=\"76\"", xaml);
        Assert.Contains("x:Name=\"SelectedBadge\"", xaml);
        Assert.Contains("x:Name=\"StateHint\"", xaml);
        Assert.DoesNotContain("x:Name=\"BtnSelect\"", xaml);
        Assert.Contains("CardRootButton.IsEnabled = !IsSelected;", code);
        Assert.Contains("ActionPill.BorderBrush", code);
        Assert.Contains("PreviewBorder", code);
        Assert.Contains("StateHint.Text", code);
    }

    [Fact]
    public void WeatherAiBackupPanels_UseCompactWorkbenchSections()
    {
        var weatherXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/WeatherPreferencesSettingsPanel.xaml"));
        var aiXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/AiImageGenerationSettingsPanel.xaml"));
        var backupXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Components/Settings/BackupSettingsPanel.xaml"));

        Assert.Contains("x:Name=\"WeatherCityCard\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherSnapshotCard\"", weatherXaml);
        Assert.Contains("x:Name=\"WeatherRecommendationCard\"", weatherXaml);
        Assert.DoesNotContain("后续天气穿搭推荐也会直接复用这里。", weatherXaml);

        Assert.Contains("x:Name=\"AiPresetCard\"", aiXaml);
        Assert.Contains("x:Name=\"AiConnectionGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCredentialCard\"", aiXaml);
        Assert.Contains("x:Name=\"AiModelGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCredentialActions\"", aiXaml);
        Assert.Contains("x:Name=\"AiStatusCard\"", aiXaml);
        Assert.Contains("x:Name=\"AiPresetGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCustomModelPanel\"", aiXaml);
        Assert.Contains("x:Name=\"AiConnectionMetaGrid\"", aiXaml);
        Assert.Contains("x:Name=\"AiCredentialActionRow\"", aiXaml);
        Assert.Contains("VerticalAlignment=\"Stretch\"", aiXaml);
        Assert.Contains("x:Name=\"AiConnectionContent\"", aiXaml);
        Assert.Contains("<Setter Property=\"MinHeight\" Value=\"74\"/>", aiXaml);
        Assert.Contains("Width=\"170\"", aiXaml);
        Assert.Contains("gpt-image-2 · 主路线", aiXaml);
        Assert.Contains("gpt-image-1.5 · 高质量", aiXaml);
        Assert.DoesNotContain("当前中转主路线", aiXaml);
        Assert.DoesNotContain("更高质量优先", aiXaml);
        Assert.DoesNotContain("让生成配置更稳定也更容易切换。", aiXaml);
        Assert.DoesNotContain("凭证与说明", aiXaml);

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
    }

    [Fact]
    public void SettingsStyles_UseUnifiedButtonsAndInputs()
    {
        var settingsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Settings.xaml"));
        var radiusXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Tokens/Radius.xaml"));
        var motionXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Tokens/Motion.xaml"));
        var buttonsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Buttons.xaml"));
        var inputsXaml = File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Inputs.xaml"));
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
        Assert.Contains("Style=\"{StaticResource SettingsFieldInput}\"", weatherXaml);
        Assert.Contains("Style=\"{StaticResource SettingsFieldComboBox}\"", weatherXaml);
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

        Assert.Contains("x:Name=\"StorageHeaderGrid\"", storageXaml);
        Assert.Contains("x:Name=\"StoragePathsGrid\"", storageXaml);
        Assert.DoesNotContain("遇到图片、数据库或缓存问题时能快速定位。", storageXaml);

        Assert.Contains("x:Name=\"ImageStatsGrid\"", imageXaml);
        Assert.Contains("x:Name=\"ImageHealthGrid\"", imageXaml);
        Assert.Contains("x:Name=\"ImageActionsWrap\"", imageXaml);
        Assert.Contains("x:Name=\"ImagePrimaryActions\"", imageXaml);
        Assert.Contains("x:Name=\"ImageDangerActions\"", imageXaml);
        Assert.Contains("x:Key=\"SettingsDangerGhostButton\"", File.ReadAllText(FindProjectFile("ClosetApp.UI/Themes/Controls/Settings.xaml")));
        Assert.Contains("Style=\"{StaticResource SettingsDangerGhostButton}\"", imageXaml);

        Assert.Contains("x:Name=\"LogHeaderGrid\"", logXaml);
        Assert.Contains("x:Name=\"LogSummaryCard\"", logXaml);
        Assert.Contains("Style=\"{StaticResource SettingsDangerGhostButton}\"", logXaml);
        Assert.DoesNotContain("查看目录并清理历史日志。", logXaml);
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
