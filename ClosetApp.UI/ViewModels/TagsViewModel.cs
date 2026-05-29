using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class TagsViewModel : ViewModelBase
{
    private static readonly string[] StatePropertyNames =
    [
        nameof(Tags),
        nameof(StyleTags),
        nameof(SceneTags),
        nameof(IsLoading),
        nameof(IsEmpty),
        nameof(IsFilteredEmpty),
        nameof(TagCount),
        nameof(FilteredCount),
        nameof(StyleCount),
        nameof(SceneCount),
        nameof(UsedCount),
        nameof(UnusedCount),
        nameof(StyleCountText),
        nameof(SceneCountText),
        nameof(UsageSummaryText),
        nameof(FilterSummary),
        nameof(HasActiveFilters),
        nameof(CollectionSectionTitle),
        nameof(CollectionSectionBody),
        nameof(ShowStyleSection),
        nameof(ShowSceneSection),
        nameof(SearchText)
    ];

    private readonly ITagService _tagService;
    private readonly TagsTabState _state = new();
    private string _searchText = string.Empty;

    public TagsViewModel(ITagService tagService)
    {
        _tagService = tagService;
    }

    public IReadOnlyList<TagListItem> Tags => _state.Tags;
    public IReadOnlyList<TagListItem> StyleTags => _state.StyleTags;
    public IReadOnlyList<TagListItem> SceneTags => _state.SceneTags;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public bool IsFilteredEmpty => _state.IsFilteredEmpty;
    public int TagCount => _state.TagCount;
    public int FilteredCount => _state.FilteredCount;
    public int StyleCount => _state.StyleCount;
    public int SceneCount => _state.SceneCount;
    public int UsedCount => _state.UsedCount;
    public int UnusedCount => _state.UnusedCount;
    public string StyleCountText => _state.StyleCountText;
    public string SceneCountText => _state.SceneCountText;
    public string UsageSummaryText => _state.UsageSummaryText;
    public string FilterSummary => _state.FilterSummary;
    public bool HasActiveFilters => _state.HasActiveFilters;
    public bool ShowStyleSection => _state.ShowStyleSection;
    public bool ShowSceneSection => _state.ShowSceneSection;
    public string CollectionSectionTitle => HasActiveFilters ? "筛选结果" : "按分类整理";
    public string CollectionSectionBody => HasActiveFilters
        ? "只显示了符合条件的标签，顺手编辑或删掉不再需要的会更干净。"
        : "把风格和场景标签分开看，更容易找到需要处理的那个。";
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
                return;

            _state.SetSearchText(value);
            NotifyStateChanged();
        }
    }

    public async Task LoadTagsAsync()
    {
        _state.BeginLoad();
        NotifyStateChanged();

        try
        {
            var tags = await _tagService.GetAllTagsAsync();
            _state.SetTags(tags);
            Log.Debug("Loaded tags. Count={TagCount}", _state.Tags.Count);
        }
        finally
        {
            NotifyStateChanged();
        }
    }

    public async Task AddTagAsync(Tag tag)
    {
        await _tagService.AddTagAsync(tag);
        await LoadTagsAsync();
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        await _tagService.UpdateTagAsync(tag);
        await LoadTagsAsync();
    }

    public async Task DeleteTagAsync(Tag tag)
    {
        await _tagService.DeleteTagAsync(tag.Id);
        await LoadTagsAsync();
    }

    public void SetSelectedCategory(TagCategory? category)
    {
        _state.SetSelectedCategory(category);
        NotifyStateChanged();
    }

    public void SetSortBy(TagSortBy sortBy)
    {
        _state.SetSortBy(sortBy);
        NotifyStateChanged();
    }

    public void SetUsageFilter(TagUsageFilter usageFilter)
    {
        _state.SetUsageFilter(usageFilter);
        NotifyStateChanged();
    }

    public void ClearFilters()
    {
        _searchText = string.Empty;
        _state.ClearFilters();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        NotifyPropertiesChanged(StatePropertyNames);
    }
}
