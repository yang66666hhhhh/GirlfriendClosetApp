using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Components.Shared.Modal;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.UI.Components.Settings;

public partial class LogMaintenanceSettingsPanel : UserControl
{
    private readonly IImageMaintenanceService _imageMaintenanceService;
    private bool _isBusy;

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

    private void OpenLogsDir_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        OpenPath(AppPaths.LogsDir);
    }

    private async void RefreshStats_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在刷新日志状态...");
            await RefreshAsync();
            ToastService.Instance.ShowInfo("统计信息已刷新。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新日志状态失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        var confirmed = await ConfirmModal.ShowAsync(
            "清理日志",
            "今天正在写入的日志会保留。",
            "只会清理历史日志文件，方便释放空间并保持日志目录整洁。确定继续吗？",
            confirmText: "清理日志");

        if (!confirmed)
            return;

        try
        {
            SetBusyState(true, "正在清理历史日志...");
            await _imageMaintenanceService.CleanupLogsAsync();
            await RefreshAsync();
            ToastService.Instance.ShowSuccess("历史日志已清理");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("清理历史日志失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void SetBusyState(bool isBusy, string? statusText = null)
    {
        _isBusy = isBusy;

        BtnOpenLogsDir.IsEnabled = !isBusy;
        BtnRefreshStats.IsEnabled = !isBusy;
        BtnClearLogs.IsEnabled = !isBusy;

        TxtLogOperationStatus.Text = statusText ?? string.Empty;
        TxtLogOperationStatus.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    }
}
