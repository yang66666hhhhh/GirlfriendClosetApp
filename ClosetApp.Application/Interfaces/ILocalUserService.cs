using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.Interfaces;

public interface ILocalUserService
{
    Task<LocalUser> EnsureInitializedAsync();
    Task<IReadOnlyList<LocalUser>> GetAllAsync();
    Task<LocalUser> GetCurrentAsync();
    Task<LocalUser> CreateMemberAsync(string displayName);
    Task<LocalUser> CreateMemberAsync(string accountName, string displayName, string initialPassword, string? initialPin = null);
    Task<LocalUser> UpdateAsync(
        Guid userId,
        string displayName,
        string? avatarPhotoPath = null,
        string? accountName = null,
        string? avatarSourcePath = null,
        bool removeAvatarPhoto = false);
    Task SwitchAsync(Guid userId);
    Task DeleteAsync(Guid userId);
}
