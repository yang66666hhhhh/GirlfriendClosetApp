using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.States;

public sealed class TagsTabState
{
    private List<Tag> _tags = new();

    public IReadOnlyList<Tag> Tags => _tags;
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _tags.Count == 0;

    public void BeginLoad() => IsLoading = true;

    public void SetTags(IEnumerable<Tag> tags)
    {
        _tags = tags.ToList();
        IsLoading = false;
    }
}
