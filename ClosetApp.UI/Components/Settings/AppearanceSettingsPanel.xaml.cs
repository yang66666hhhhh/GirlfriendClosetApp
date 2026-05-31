using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
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
        Loaded += (_, _) => Refresh();
    }

    public void Refresh()
    {
        TxtVersion.Text = $"版本 {GetVersion()}";
        ApplyThemeCardSelection(_themeService.CurrentTheme);
    }

    public event EventHandler<AppThemeKind>? ThemeChanged;

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

    private void OpenAppDir_Click(object sender, RoutedEventArgs e)
    {
        OpenPath(AppDomain.CurrentDomain.BaseDirectory);
    }
}
