namespace ClosetApp.UI.Components.Clothing;

public enum ClothingEditorResultType
{
    Saved,
    Deleted,
    Cancelled
}

public sealed record ClothingEditorResult(
    ClothingEditorResultType Type,
    global::ClosetApp.Domain.Entities.Clothing? Clothing = null
);
