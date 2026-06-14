using ClosetApp.UI.Services;
using System.Windows;

namespace ClosetApp.UI.Components.Shared.Modal;

public static class ConfirmModal
{
    public static Task<bool> ShowAsync(
        string title,
        string body,
        string detail,
        string confirmText = "确认",
        string cancelText = "取消",
        string confirmStyleKey = "ModalSaveButton",
        bool showCancel = true)
    {
        var dialog = new ConfirmDialog
        {
            Title = title,
            Body = body,
            Detail = detail,
            ConfirmText = confirmText,
            CancelText = cancelText,
            IsCancelVisible = showCancel,
            ConfirmButtonStyle = (Style)System.Windows.Application.Current.FindResource(confirmStyleKey)
        };

        var tcs = new TaskCompletionSource<bool>();
        void ConfirmedHandler(object? sender, EventArgs e)
        {
            Cleanup();
            tcs.TrySetResult(true);
        }

        void CancelledHandler(object? sender, EventArgs e)
        {
            Cleanup();
            tcs.TrySetResult(false);
        }

        void Cleanup()
        {
            dialog.Confirmed -= ConfirmedHandler;
            dialog.Cancelled -= CancelledHandler;
            ModalService.Instance.Hide();
        }

        dialog.Confirmed += ConfirmedHandler;
        dialog.Cancelled += CancelledHandler;
        ModalService.Instance.Show(dialog);
        return tcs.Task;
    }

    public static Task<bool> ShowDeleteAsync(
        string detail,
        string title = "确认删除",
        string body = "删除后无法恢复。",
        string confirmText = "删除",
        string cancelText = "取消")
    {
        return ShowAsync(title, body, detail, confirmText, cancelText, confirmStyleKey: "ModalDangerButton");
    }

    public static Task ShowMessageAsync(
        string title,
        string body,
        string detail,
        string confirmText = "我知道了",
        string confirmStyleKey = "ModalSaveButton")
    {
        return ShowAsync(
            title,
            body,
            detail,
            confirmText,
            cancelText: string.Empty,
            confirmStyleKey: confirmStyleKey,
            showCancel: false);
    }
}
