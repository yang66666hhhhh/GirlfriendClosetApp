using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.UseCases.Tags;

public sealed class GetTagsForSelection
{
    private readonly ITagService _tagService;

    public GetTagsForSelection(ITagService tagService)
    {
        _tagService = tagService;
    }

    public Task<IEnumerable<Tag>> ExecuteAsync(TagCategory category)
    {
        return _tagService.GetTagsByCategoryAsync(category);
    }
}
