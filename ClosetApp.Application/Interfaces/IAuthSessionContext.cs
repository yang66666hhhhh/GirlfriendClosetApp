namespace ClosetApp.Application.Interfaces;

public sealed class AuthSessionChangedEventArgs : EventArgs
{
    public AuthSessionChangedEventArgs(bool isAuthenticated, Guid? userId)
    {
        IsAuthenticated = isAuthenticated;
        UserId = userId;
    }

    public bool IsAuthenticated { get; }
    public Guid? UserId { get; }
}

public interface IAuthSessionContext
{
    event EventHandler<AuthSessionChangedEventArgs>? AuthSessionChanged;

    bool IsAuthenticated { get; }
    Guid? AuthenticatedUserId { get; }
    void MarkAuthenticated(Guid userId);
    void Clear();
}
