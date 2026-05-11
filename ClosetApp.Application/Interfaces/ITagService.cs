using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<IEnumerable<Tag>> GetStyleTagsAsync();
    Task<IEnumerable<Tag>> GetTagsByCategoryAsync(TagCategory category);
    Task<Tag> AddTagAsync(Tag tag);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(Guid id);
}
