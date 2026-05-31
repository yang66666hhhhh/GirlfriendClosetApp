using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class LogMaintenanceSettingsPanel : UserControl
{
    private readonly IImageMaintenanceService _imageMaintenanceService;

    public LogMaintenanceSettingsPanel()
    {
        InitializeComponent();
        _imageMaintenanceService = App.Services.GetRequiredService<IImageMaintenanceService>();
        TxtLogDir.Text = AppPaths.LogsDir;
    }

    private SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

    public Task RefreshAsync()
    {
        return ViewModel.RefreshStatsAsync();
    }

    private static void OpenPath(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void OpenLogsDir_Click(object sender, RoutedEventArgs e) => OpenPath(AppPaths.LogsDir);

    private async void RefreshStats_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync();
        ToastService.Instance.ShowInfo("统计信息已刷新。");
    }

    private async void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "确定清理历史日志吗？今天正在写入的日志会保留。",
            "清理日志",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.OK)
            return;

        await _imageMaintenanceService.CleanupLogsAsync();
        await RefreshAsync();
        MessageBox.Show("历史日志已清理。", "完成", MessageBoxButton.OK, MessageBoxImage.Information);
        ToastService.Instance.ShowSuccess("历史日志已清理");
    }
}
