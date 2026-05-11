namespace ClosetApp.UI.Components.Tags.Controls;

public enum TagEditorResultType { Saved, Cancelled }

public sealed class TagEditorResult(TagEditorResultType type, ClosetApp.Domain.Entities.Tag? tag)
{
    public TagEditorResultType Type { get; } = type;
    public ClosetApp.Domain.Entities.Tag? Tag { get; } = tag;
}