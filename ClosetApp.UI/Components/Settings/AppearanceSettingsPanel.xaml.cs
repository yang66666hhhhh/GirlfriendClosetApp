using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.ViewModels;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class AppearanceSettingsPanel : UserControl
{
    private readonly ThemeService _themeService;
    private bool _isApplyingSelection;

    public AppearanceSettingsPanel()
    {
        InitializeComponent();
        _themeService = App.Services.GetRequiredService<ThemeService>();
    }

    public void Refresh()
    {
        TxtVersion.Text = $"版本 {GetVersion()}";
        ApplyThemeCardSelection(_themeService.CurrentTheme);
        if (DataContext is SettingsViewModel viewModel)
        {
            ApplyOutfitCardDisplaySelection(viewModel.DefaultOutfitCardDisplayMode);
            ApplyFontSizeSelection(viewModel.FontSizeLevel);
        }
    }

    public event EventHandler<AppThemeKind>? ThemeChanged;
    public event EventHandler<OutfitCardDisplayMode>? OutfitCardDisplayModeChanged;
    public event EventHandler<AppFontSizeLevel>? FontSizeLevelChanged;

    private static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version == null ? "开发版" : $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static void OpenPath(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void ThemeCard_Selected(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is not Components.Shared.ThemeCard card)
            return;
        ThemeChanged?.Invoke(this, card.ThemeKind);
    }

    public void ApplyThemeCardSelection(AppThemeKind theme)
    {
        ThemeRoseCard.IsSelected = theme == AppThemeKind.Rose;
        ThemeBlueCard.IsSelected = theme == AppThemeKind.Blue;
    }

    public void ApplyOutfitCardDisplaySelection(OutfitCardDisplayMode mode)
    {
        ApplySelectionSilently(() =>
        {
            RadioOutfitFirst.IsChecked = mode == OutfitCardDisplayMode.OutfitFirst;
            RadioEffectImageFirst.IsChecked = mode == OutfitCardDisplayMode.EffectImageFirst;
        });
    }

    public void ApplyFontSizeSelection(AppFontSizeLevel level)
    {
        ApplySelectionSilently(() =>
        {
            RadioFontSmall.IsChecked = level == AppFontSizeLevel.Small;
            RadioFontStandard.IsChecked = level == AppFontSizeLevel.Standard;
            RadioFontComfortable.IsChecked = level == AppFontSizeLevel.Comfortable;
            RadioFontLarge.IsChecked = level == AppFontSizeLevel.Large;
            RadioFontExtraLarge.IsChecked = level == AppFontSizeLevel.ExtraLarge;
        });
    }

    private void ApplySelectionSilently(Action apply)
    {
        var wasApplying = _isApplyingSelection;
        _isApplyingSelection = true;
        try
        {
            apply();
        }
        finally
        {
            _isApplyingSelection = wasApplying;
        }
    }

    private void OpenAppDir_Click(object sender, RoutedEventArgs e)
    {
        OpenPath(AppDomain.CurrentDomain.BaseDirectory);
    }

    private void OutfitDisplayMode_Checked(object sender, RoutedEventArgs e)
    {
        if (_isApplyingSelection)
            return;

        if (sender is not RadioButton radioButton || radioButton.IsChecked != true)
            return;

        if (Equals(radioButton.Content, "效果图优先"))
        {
            OutfitCardDisplayModeChanged?.Invoke(this, OutfitCardDisplayMode.EffectImageFirst);
            return;
        }

        OutfitCardDisplayModeChanged?.Invoke(this, OutfitCardDisplayMode.OutfitFirst);
    }

    private void FontSizeLevel_Checked(object sender, RoutedEventArgs e)
    {
        if (_isApplyingSelection)
            return;

        if (sender is not RadioButton { IsChecked: true, Tag: string levelName })
            return;

        if (!Enum.TryParse<AppFontSizeLevel>(levelName, out var level))
            return;

        FontSizeLevelChanged?.Invoke(this, level);
    }
}
