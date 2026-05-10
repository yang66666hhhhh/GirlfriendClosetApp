using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.Interfaces;

public interface ITagService
{
    Task<IEnumerable<Tag>> GetAllTagsAsync();
    Task<IEnumerable<Tag>> GetStyleTagsAsync();
    Task<Tag> AddTagAsync(Tag tag);
    Task UpdateTagAsync(Tag tag);
    Task DeleteTagAsync(Guid id);
}