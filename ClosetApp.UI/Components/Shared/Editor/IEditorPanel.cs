namespace ClosetApp.UI.Components.Shared.Editor;

public interface IEditorPanel<T>
{
    event EventHandler<EditorResult<T>>? EditorCompleted;
}
