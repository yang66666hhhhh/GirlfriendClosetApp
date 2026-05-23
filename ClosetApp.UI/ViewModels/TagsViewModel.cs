using CommunityToolkit.Mvvm.ComponentModel;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.States;
using Serilog;

namespace ClosetApp.UI.ViewModels;

public partial class TagsViewModel : ObservableObject
{
    private readonly ITagService _tagService;
    private readonly TagsTabState _state = new();

    public TagsViewModel(ITagService tagService)
    {
        _tagService = tagService;
    }

    public IReadOnlyList<Tag> Tags => _state.Tags;
    public bool IsLoading => _state.IsLoading;
    public bool IsEmpty => _state.IsEmpty;
    public int TagCount => _state.TagCount;
    public string TagCountText => $"{TagCount} 个标签";
    public string CategorySummaryText => _state.CategorySummaryText;
    public string CollectionSectionTitle => "全部标签";
    public string CollectionSectionBody => "右键可以编辑或删除，先把常用风格词整理顺，衣柜和搭配会一起受益。";

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

    private void NotifyStateChanged()
    {
        OnPropertyChanged(nameof(Tags));
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(TagCount));
        OnPropertyChanged(nameof(TagCountText));
        OnPropertyChanged(nameof(CategorySummaryText));
        OnPropertyChanged(nameof(CollectionSectionTitle));
        OnPropertyChanged(nameof(CollectionSectionBody));
    }
}
