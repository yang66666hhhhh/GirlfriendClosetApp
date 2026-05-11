using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class TagService : ITagService
{
    private readonly ITagRepository _repository;

    public TagService(ITagRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Tag>> GetAllTagsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<IEnumerable<Tag>> GetStyleTagsAsync()
    {
        return await GetTagsByCategoryAsync(TagCategory.Style);
    }

    public async Task<IEnumerable<Tag>> GetTagsByCategoryAsync(TagCategory category)
    {
        return await _repository.GetByCategoryAsync(category);
    }

    public async Task<Tag> AddTagAsync(Tag tag)
    {
        await _repository.AddAsync(tag);
        return tag;
    }

    public async Task UpdateTagAsync(Tag tag)
    {
        await _repository.UpdateAsync(tag);
    }

    public async Task DeleteTagAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }
}
