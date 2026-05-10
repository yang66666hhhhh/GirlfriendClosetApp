using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using System.Collections.ObjectModel;

namespace ClosetApp.UI.ViewModels;

public partial class TagsViewModel : ObservableObject
{
    private readonly ITagService _tagService;

    [ObservableProperty]
    private ObservableCollection<Tag> _tags = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty = true;

    public TagsViewModel(ITagService tagService)
    {
        _tagService = tagService;
    }

    [RelayCommand]
    public async Task LoadTagsAsync()
    {
        IsLoading = true;
        try
        {
            var tags = await _tagService.GetAllTagsAsync();
            Tags = new ObservableCollection<Tag>(tags);
            IsEmpty = Tags.Count == 0;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task AddTagAsync(Tag tag)
    {
        await _tagService.AddTagAsync(tag);
        await LoadTagsAsync();
    }

    [RelayCommand]
    public async Task DeleteTagAsync(Guid id)
    {
        await _tagService.DeleteTagAsync(id);
        await LoadTagsAsync();
    }
}