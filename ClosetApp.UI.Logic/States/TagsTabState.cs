using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Logic.States;

public enum TagSortBy
{
    MostUsed,
    Name,
    LeastUsed
}

public sealed class TagListItem
{
    public TagListItem(Tag tag)
    {
        Tag = tag;
        UsageCount = tag.ClothingTags?.Count ?? 0;
    }

    public Tag Tag { get; }
    public string Name => Tag.Name;
    public string Color => Tag.Color;
    public TagCategory Category => Tag.Category;
    public int UsageCount { get; }
    public bool IsUnused => UsageCount == 0;
    public string UsageText => IsUnused ? "还没用到衣物上" : $"{UsageCount} 件衣物在用";
}

public sealed class TagsTabState
{
    private List<TagListItem> _allTags = new();
    private List<TagListItem> _filteredTags = new();
    private List<TagListItem> _styleTags = new();
    private List<TagListItem> _sceneTags = new();
    private List<TagListItem> _seasonTags = new();
    private string _searchText = string.Empty;

    public IReadOnlyList<TagListItem> Tags => _filteredTags;
    public IReadOnlyList<TagListItem> StyleTags => _styleTags;
    public IReadOnlyList<TagListItem> SceneTags => _sceneTags;
    public IReadOnlyList<TagListItem> SeasonTags => _seasonTags;
    public bool IsLoading { get; private set; }
    public bool IsEmpty => _allTags.Count == 0;
    public bool IsFilteredEmpty => _filteredTags.Count == 0;
    public int TagCount => _allTags.Count;
    public int FilteredCount => _filteredTags.Count;
    public int StyleCount => _allTags.Count(tag => tag.Category == TagCategory.Style);
    public int SceneCount => _allTags.Count(tag => tag.Category == TagCategory.Scene);
    public int SeasonCount => _allTags.Count(tag => tag.Category == TagCategory.Season);
    public int UsedCount => _allTags.Count(tag => tag.UsageCount > 0);
    public int UnusedCount => _allTags.Count(tag => tag.UsageCount == 0);
    public TagCategory? SelectedCategory { get; private set; }
    public TagSortBy SortBy { get; private set; } = TagSortBy.MostUsed;
    public bool HasActiveFilters => !string.IsNullOrWhiteSpace(_searchText) || SelectedCategory.HasValue;
    public bool ShowStyleSection => ShouldShowSection(TagCategory.Style, _styleTags.Count);
    public bool ShowSceneSection => ShouldShowSection(TagCategory.Scene, _sceneTags.Count);
    public bool ShowSeasonSection => ShouldShowSection(TagCategory.Season, _seasonTags.Count);
    public string CategorySummaryText =>
        $"风格 {StyleCount} · 场景 {SceneCount} · 季节 {SeasonCount}";
    public string UsageSummaryText =>
        $"已用 {UsedCount} · 待整理 {UnusedCount}";
    public string FilterSummary => BuildFilterSummary();

    public void BeginLoad() => IsLoading = true;

    public void SetTags(IEnumerable<Tag> tags)
    {
        _allTags = tags
            .Select(tag => new TagListItem(tag))
            .ToList();
        ApplyFilters();
        IsLoading = false;
    }

    public void SetSearchText(string searchText)
    {
        _searchText = searchText?.Trim() ?? string.Empty;
        ApplyFilters();
    }

    public void SetSelectedCategory(TagCategory? category)
    {
        SelectedCategory = category;
        ApplyFilters();
    }

    public void SetSortBy(TagSortBy sortBy)
    {
        SortBy = sortBy;
        ApplyFilters();
    }

    public void ClearFilters()
    {
        _searchText = string.Empty;
        SelectedCategory = null;
        ApplyFilters();
    }

    // 统一在这里处理搜索、分类和排序，避免页面层拼装条件。
    private void ApplyFilters()
    {
        IEnumerable<TagListItem> query = _allTags;

        if (!string.IsNullOrWhiteSpace(_searchText))
            query = query.Where(tag => tag.Name.Contains(_searchText, StringComparison.CurrentCultureIgnoreCase));

        if (SelectedCategory.HasValue)
            query = query.Where(tag => tag.Category == SelectedCategory.Value);

        _filteredTags = Sort(query).ToList();
        _styleTags = _filteredTags.Where(tag => tag.Category == TagCategory.Style).ToList();
        _sceneTags = _filteredTags.Where(tag => tag.Category == TagCategory.Scene).ToList();
        _seasonTags = _filteredTags.Where(tag => tag.Category == TagCategory.Season).ToList();
        IsLoading = false;
    }

    private IEnumerable<TagListItem> Sort(IEnumerable<TagListItem> query) => SortBy switch
    {
        TagSortBy.Name => query
            .OrderBy(tag => GetCategoryOrder(tag.Category))
            .ThenBy(tag => tag.Name, StringComparer.CurrentCulture)
            .ThenByDescending(tag => tag.UsageCount),
        TagSortBy.LeastUsed => query
            .OrderBy(tag => GetCategoryOrder(tag.Category))
            .ThenBy(tag => tag.UsageCount)
            .ThenBy(tag => tag.Name, StringComparer.CurrentCulture),
        _ => query
            .OrderBy(tag => GetCategoryOrder(tag.Category))
            .ThenByDescending(tag => tag.UsageCount)
            .ThenBy(tag => tag.Name, StringComparer.CurrentCulture)
    };

    private bool ShouldShowSection(TagCategory category, int count) =>
        count > 0 && (!SelectedCategory.HasValue || SelectedCategory == category);

    private string BuildFilterSummary()
    {
        if (!HasActiveFilters)
            return $"全部标签 · {FilteredCount} 个";

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(_searchText))
            parts.Add($"搜索“{_searchText}”");
        if (SelectedCategory.HasValue)
            parts.Add(GetCategoryLabel(SelectedCategory.Value));

        return $"{string.Join(" · ", parts)} · {FilteredCount} 个结果";
    }

    private static string GetCategoryLabel(TagCategory category) => category switch
    {
        TagCategory.Style => "风格标签",
        TagCategory.Scene => "场景标签",
        TagCategory.Season => "季节标签",
        _ => category.ToString()
    };

    private static int GetCategoryOrder(TagCategory category) => category switch
    {
        TagCategory.Style => 0,
        TagCategory.Scene => 1,
        TagCategory.Season => 2,
        _ => 3
    };
}
