using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.States;

public sealed class TagsTabState
{
    private List<Tag> _tags = new();

    public IReadOnlyList<Tag> Tags => _tags;
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _tags.Count == 0;
    public int TagCount => _tags.Count;
    public int StyleCount => _tags.Count(tag => tag.Category == TagCategory.Style);
    public int SceneCount => _tags.Count(tag => tag.Category == TagCategory.Scene);
    public int SeasonCount => _tags.Count(tag => tag.Category == TagCategory.Season);
    public string CategorySummaryText =>
        $"风格 {StyleCount} · 场景 {SceneCount} · 季节 {SeasonCount}";

    public void BeginLoad() => IsLoading = true;

    public void SetTags(IEnumerable<Tag> tags)
    {
        _tags = tags
            .OrderBy(tag => tag.Category)
            .ThenBy(tag => tag.Name)
            .ToList();
        IsLoading = false;
    }
}
