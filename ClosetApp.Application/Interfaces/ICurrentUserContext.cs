namespace ClosetApp.Application.Interfaces;

public sealed class CurrentUserChangedEventArgs : EventArgs
{
    public CurrentUserChangedEventArgs(Guid userId)
    {
        UserId = userId;
    }

    public Guid UserId { get; }
}

public interface ICurrentUserContext
{
    event EventHandler<CurrentUserChangedEventArgs>? CurrentUserChanged;

    Task<Guid?> GetCurrentUserIdAsync();
    Task<Guid> GetRequiredCurrentUserIdAsync();
    Task<Guid> GetRequiredStoredUserIdAsync();
    Task SetCurrentUserIdAsync(Guid userId);
}
