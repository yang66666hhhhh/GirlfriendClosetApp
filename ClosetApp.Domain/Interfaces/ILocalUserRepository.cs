using ClosetApp.Domain.Entities;

namespace ClosetApp.Domain.Interfaces;

public interface ILocalUserRepository : IRepository<LocalUser>
{
    Task<LocalUser?> GetSuperAdminAsync();
    Task<LocalUser?> GetActiveByIdAsync(Guid id);
    Task<LocalUser?> GetActiveByAccountNameAsync(string accountName);
    Task<int> CountActiveAsync();
    Task AssignUnownedWorkspaceAsync(Guid userId);
    Task EnsureDefaultTagsAsync(Guid userId);
    Task DeleteWorkspaceAsync(Guid userId);
}
