using System.Text.Json;
using ClosetApp.Application.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;
    private readonly IAuthSessionContext? _authSessionContext;

    public CurrentUserContext(string? filePath = null, IAuthSessionContext? authSessionContext = null)
    {
        _filePath = filePath ?? Path.Combine(AppPaths.BaseDir, "current-user.json");
        _authSessionContext = authSessionContext;
    }

    public event EventHandler<CurrentUserChangedEventArgs>? CurrentUserChanged;

    public async Task<Guid?> GetCurrentUserIdAsync()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            await using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var document = await JsonSerializer.DeserializeAsync<CurrentUserDocument>(stream, JsonOptions).ConfigureAwait(false);
            return document?.CurrentUserId == Guid.Empty ? null : document?.CurrentUserId;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Guid> GetRequiredCurrentUserIdAsync()
    {
        if (_authSessionContext is { IsAuthenticated: false })
            throw new InvalidOperationException("当前尚未登录。");

        var userId = await GetCurrentUserIdAsync().ConfigureAwait(false);
        return userId ?? throw new InvalidOperationException("当前用户尚未初始化。");
    }

    public async Task<Guid> GetRequiredStoredUserIdAsync()
    {
        var userId = await GetCurrentUserIdAsync().ConfigureAwait(false);
        return userId ?? throw new InvalidOperationException("当前用户尚未初始化。");
    }

    public async Task SetCurrentUserIdAsync(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("当前用户 ID 不能为空。", nameof(userId));

        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using (var stream = File.Create(_filePath))
        {
            await JsonSerializer.SerializeAsync(stream, new CurrentUserDocument { CurrentUserId = userId }, JsonOptions).ConfigureAwait(false);
        }

        CurrentUserChanged?.Invoke(this, new CurrentUserChangedEventArgs(userId));
    }

    private sealed class CurrentUserDocument
    {
        public Guid CurrentUserId { get; set; }
    }
}
