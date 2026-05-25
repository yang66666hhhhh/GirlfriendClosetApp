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
                await onCompleted(result);
                await Task.Delay(600);
                ModalService.Instance.Hide();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EditorModal] onCompleted exception: {ex}");
                ToastService.Instance.ShowError("操作失败", ex.Message);
            }
        };

        ModalService.Instance.Show(panel);
    }
}
