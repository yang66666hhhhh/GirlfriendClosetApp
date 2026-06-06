using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class AppearanceSettingsPanel : UserControl
{
    private readonly ThemeService _themeService;
    private readonly SettingsViewModel _viewModel;

    public AppearanceSettingsPanel()
    {
        InitializeComponent();
        _themeService = App.Services.GetRequiredService<ThemeService>();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }

    public void Refresh()
    {
        TxtVersion.Text = $"版本 {GetVersion()}";
        ApplyThemeCardSelection(_themeService.CurrentTheme);
        ApplyOutfitCardDisplaySelection(_viewModel.DefaultOutfitCardDisplayMode);
    }

    public event EventHandler<AppThemeKind>? ThemeChanged;
    public event EventHandler<OutfitCardDisplayMode>? OutfitCardDisplayModeChanged;

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
        RadioOutfitFirst.IsChecked = mode == OutfitCardDisplayMode.OutfitFirst;
        RadioEffectImageFirst.IsChecked = mode == OutfitCardDisplayMode.EffectImageFirst;
    }

    private void OpenAppDir_Click(object sender, RoutedEventArgs e)
    {
        OpenPath(AppDomain.CurrentDomain.BaseDirectory);
    }

    private void OutfitDisplayMode_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is not RadioButton radioButton || radioButton.IsChecked != true)
            return;

        if (Equals(radioButton.Content, "效果图优先"))
        {
            OutfitCardDisplayModeChanged?.Invoke(this, OutfitCardDisplayMode.EffectImageFirst);
            return;
        }

        OutfitCardDisplayModeChanged?.Invoke(this, OutfitCardDisplayMode.OutfitFirst);
    }
}
