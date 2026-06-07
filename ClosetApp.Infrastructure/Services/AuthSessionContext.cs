using ClosetApp.Application.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class AuthSessionContext : IAuthSessionContext
{
    public event EventHandler<AuthSessionChangedEventArgs>? AuthSessionChanged;

    public bool IsAuthenticated { get; private set; }
    public Guid? AuthenticatedUserId { get; private set; }

    public void MarkAuthenticated(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("认证用户 ID 不能为空。", nameof(userId));

        IsAuthenticated = true;
        AuthenticatedUserId = userId;
        AuthSessionChanged?.Invoke(this, new AuthSessionChangedEventArgs(true, userId));
    }

    public void Clear()
    {
        IsAuthenticated = false;
        AuthenticatedUserId = null;
        AuthSessionChanged?.Invoke(this, new AuthSessionChangedEventArgs(false, null));
    }
}
