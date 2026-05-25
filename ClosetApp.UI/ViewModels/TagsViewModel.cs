using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class TagsViewModel : ObservableObject
{
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
    public IReadOnlyList<TagListItem> SeasonTags => _state.SeasonTags;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public bool IsFilteredEmpty => _state.IsFilteredEmpty;
    public int TagCount => _state.TagCount;
    public int FilteredCount => _state.FilteredCount;
    public int UsedCount => _state.UsedCount;
    public int UnusedCount => _state.UnusedCount;
    public string TagCountText => $"{TagCount} 个标签";
    public string UsedCountText => $"{UsedCount} 个已在使用";
    public string UnusedCountText => $"{UnusedCount} 个待整理";
    public string CategorySummaryText => _state.CategorySummaryText;
    public string UsageSummaryText => _state.UsageSummaryText;
    public string FilterSummary => _state.FilterSummary;
    public bool HasActiveFilters => _state.HasActiveFilters;
    public bool ShowStyleSection => _state.ShowStyleSection;
    public bool ShowSceneSection => _state.ShowSceneSection;
    public bool ShowSeasonSection => _state.ShowSeasonSection;
    public string CollectionSectionTitle => HasActiveFilters ? "筛选结果" : "按分类整理";
    public string CollectionSectionBody => HasActiveFilters
        ? "现在只保留了符合条件的标签，顺手编辑或删掉不再需要的会更干净。"
        : "把风格、场景和季节词分开看，会更容易补齐空缺，也更方便后面做筛选。";
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

    public void ClearFilters()
    {
        _searchText = string.Empty;
        _state.ClearFilters();
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(StyleTags));
        OnPropertyChanged(nameof(SceneTags));
        OnPropertyChanged(nameof(SeasonTags));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsFilteredEmpty));
        OnPropertyChanged(nameof(TagCount));
        OnPropertyChanged(nameof(FilteredCount));
        OnPropertyChanged(nameof(UsedCount));
        OnPropertyChanged(nameof(UnusedCount));
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(UsedCountText));
        OnPropertyChanged(nameof(UnusedCountText));
        OnPropertyChanged(nameof(CategorySummaryText));
        OnPropertyChanged(nameof(UsageSummaryText));
        OnPropertyChanged(nameof(FilterSummary));
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(CollectionSectionTitle));
        OnPropertyChanged(nameof(CollectionSectionBody));
        OnPropertyChanged(nameof(ShowStyleSection));
        OnPropertyChanged(nameof(ShowSceneSection));
        OnPropertyChanged(nameof(ShowSeasonSection));
        OnPropertyChanged(nameof(SearchText));
    }
}
