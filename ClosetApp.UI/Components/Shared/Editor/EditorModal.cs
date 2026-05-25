using System.Diagnostics;
using System.Windows.Controls;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Shared.Editor;

public static class EditorModal
{
    public static void Show<T>(IEditorPanel<T> panel, Func<EditorResult<T>, Task> onCompleted)
        where T : class
    {
        if (panel is not UserControl userControl)
            throw new ArgumentException("Panel must be a UserControl.", nameof(panel));

        Show(userControl, onCompleted);
    }

    public static void Show<T>(UserControl panel, Func<EditorResult<T>, Task> onCompleted)
        where T : class
    {
        if (panel is not IEditorPanel<T> editorPanel)
            throw new ArgumentException("Panel must implement IEditorPanel<T>.", nameof(panel));

        editorPanel.EditorCompleted += async (_, result) =>
        {
            try
            {
                Debug.WriteLine($"[EditorModal] onCompleted start, result={result.Type}");
                await onCompleted(result);
                Debug.WriteLine("[EditorModal] onCompleted success, delaying 600ms...");
                await Task.Delay(600);
                Debug.WriteLine("[EditorModal] Hiding modal");
                ModalService.Instance.Hide();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditorModal] onCompleted exception: {ex.Message}");
                ToastService.Instance.ShowError("操作失败", ex.Message);
            }
        };

        ModalService.Instance.Show(panel);
    }
}
