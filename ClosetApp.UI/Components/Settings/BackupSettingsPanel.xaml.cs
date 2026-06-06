using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ClosetApp.Application.DTOs;
using ClosetApp.Infrastructure;
using ClosetApp.UI.Services;
using ClosetApp.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ClosetApp.UI.Components.Settings;

public partial class BackupSettingsPanel : UserControl
{
    private readonly SettingsViewModel _viewModel;
    private bool _isBusy;

    public BackupSettingsPanel()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<SettingsViewModel>();
    }

    public event EventHandler? BackupImported;
    public event EventHandler? RepairMissingImagesRequested;

    public Task RefreshAsync()
    {
        return _viewModel.RefreshBackupStateAsync();
    }

    private static void OpenPath(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private static void RevealFile(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrWhiteSpace(directory))
            return;

        Directory.CreateDirectory(directory);
        if (File.Exists(filePath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
            return;
        }

        OpenPath(directory);
    }

    private void OpenBackupsDir_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        OpenPath(AppPaths.BackupsDir);
    }

    private async void ExportBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        var dialog = new SaveFileDialog
        {
            Filter = "ZIP 备份包|*.zip|JSON 备份|*.json",
            DefaultExt = ".zip",
            FileName = Path.GetFileName(_viewModel.BuildDefaultBackupPath()),
            InitialDirectory = AppPaths.BackupsDir
        };

        if (dialog.ShowDialog() != true)
            return;

        var validation = await _viewModel.ValidateBackupExportAsync(dialog.FileName);
        if (!ConfirmExport(validation))
            return;

        try
        {
            SetBusyState(true, "正在导出备份...");
            var result = await _viewModel.ExportBackupWithFeedbackAsync(dialog.FileName);
            ToastService.Instance.ShowSuccess("备份已导出", Path.GetFileName(result.FilePath));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("导出备份失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void QuickExportBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        var filePath = _viewModel.BuildDefaultBackupPath();
        var validation = await _viewModel.ValidateBackupExportAsync(filePath);
        if (!ConfirmExport(validation))
            return;

        try
        {
            SetBusyState(true, "正在快速导出备份...");
            var result = await _viewModel.ExportBackupWithFeedbackAsync(filePath);
            ToastService.Instance.ShowSuccess("备份已导出", Path.GetFileName(result.FilePath));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("快速导出失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void ImportBackup_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        var dialog = new OpenFileDialog
        {
            Filter = "备份文件|*.zip;*.json|ZIP 备份包|*.zip|JSON 备份|*.json",
            CheckFileExists = true,
            InitialDirectory = AppPaths.BackupsDir
        };

        if (dialog.ShowDialog() != true)
            return;

        var confirm = MessageBox.Show(
            "导入会覆盖当前数据库中的衣服、搭配、标签和穿着记录。ZIP 备份包会同时恢复图片，旧版 JSON 只恢复核心数据，确定继续吗？",
            "确认导入备份",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        try
        {
            SetBusyState(true, "正在导入备份...");
            var result = await _viewModel.ImportBackupWithFeedbackAsync(dialog.FileName);
            BackupImported?.Invoke(this, EventArgs.Empty);
            if (!result.Success)
                ToastService.Instance.ShowError("导入备份失败", BuildImportMessage(result));
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("导入备份失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void RefreshBackupState_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        try
        {
            SetBusyState(true, "正在刷新备份状态...");
            await RefreshAsync();
            ToastService.Instance.ShowInfo("备份状态已刷新。");
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("刷新备份状态失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private async void ClearBackupHistory_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        var confirm = MessageBox.Show(
            "确定清空备份历史吗？这不会删除已经导出的备份文件。",
            "清空备份历史",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.OK)
            return;

        try
        {
            SetBusyState(true, "正在清空备份历史...");
            await _viewModel.ClearBackupHistoryWithFeedbackAsync();
        }
        catch (Exception ex)
        {
            ToastService.Instance.ShowError("清空备份历史失败", ex.Message);
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void RepairMissingImages_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy)
            return;

        RepairMissingImagesRequested?.Invoke(this, EventArgs.Empty);
    }

    private void OpenBackupFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string filePath } || string.IsNullOrWhiteSpace(filePath))
            return;
        RevealFile(filePath);
    }

    private void OpenBackupFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { Tag: string filePath } || string.IsNullOrWhiteSpace(filePath))
            return;
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            OpenPath(directory);
    }

    private static bool ConfirmExport(BackupValidationResult validation)
    {
        if (!validation.HasWarnings)
            return true;

        var message = "导出前提醒：\n\n" + string.Join("\n", validation.Warnings) + "\n\n确定继续导出吗？";
        return MessageBox.Show(
            message,
            "确认导出备份",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning) == MessageBoxResult.OK;
    }

    private static string BuildImportMessage(BackupImportResult result)
    {
        var message = result.Summary;
        if (result.Warnings.Count > 0)
            message += $"\n\n提醒：{string.Join(" ", result.Warnings)}";
        if (!result.Success && !string.IsNullOrWhiteSpace(result.FailureDetail))
            message += $"\n\n失败原因：{result.FailureDetail}";
        return message;
    }

    private void SetBusyState(bool isBusy, string? statusText = null)
    {
        _isBusy = isBusy;

        BtnRefreshBackupState.IsEnabled = !isBusy;
        BtnExportBackup.IsEnabled = !isBusy;
        BtnQuickExportBackup.IsEnabled = !isBusy;
        BtnOpenBackupsDir.IsEnabled = !isBusy;
        BtnImportBackup.IsEnabled = !isBusy;
        BtnRepairMissingImages.IsEnabled = !isBusy;
        BtnClearBackupHistory.IsEnabled = !isBusy;

        TxtBackupOperationStatus.Text = statusText ?? string.Empty;
        TxtBackupOperationStatus.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
    }
}
