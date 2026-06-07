using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.Interfaces;

public sealed class LocalLoginResult
{
    private LocalLoginResult(bool success, LocalUser? user, string? errorMessage)
    {
        Success = success;
        User = user;
        ErrorMessage = errorMessage;
    }

    public bool Success { get; }
    public LocalUser? User { get; }
    public string? ErrorMessage { get; }

    public static LocalLoginResult Ok(LocalUser user) => new(true, user, null);
    public static LocalLoginResult Failed(string message) => new(false, null, message);
}

public interface ILocalAuthService
{
    Task<bool> HasAnyCredentialAsync();
    Task<bool> HasCredentialAsync(Guid userId);
    Task SetPasswordAsync(Guid userId, string password);
    Task SetPinAsync(Guid userId, string? pin);
    Task<LocalLoginResult> LoginAsync(Guid userId, string secret, LocalCredentialKind kind);
    Task<LocalLoginResult> LoginAsync(string accountName, string secret, LocalCredentialKind kind);
    Task LogoutAsync();
    Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword);
    Task UpdateOwnCredentialAsync(Guid userId, string newPassword, string? newPin = null);
    Task ResetMemberCredentialAsync(Guid userId, string newPassword, string? newPin = null);
}
