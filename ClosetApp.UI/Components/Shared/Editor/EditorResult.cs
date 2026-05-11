namespace ClosetApp.UI.Components.Shared.Editor;

public enum EditorResultType
{
    Saved,
    Deleted,
    Cancelled
}

public sealed record EditorResult<T>(
    EditorResultType Type,
    T? Entity = default
);
