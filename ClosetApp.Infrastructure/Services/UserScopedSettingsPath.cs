using ClosetApp.Application.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class UserScopedSettingsPath
{
    private readonly ICurrentUserContext? _currentUserContext;
    private readonly string _globalFilePath;
    private readonly string _baseDirectory;
    private readonly string _fileName;

    public UserScopedSettingsPath(ICurrentUserContext? currentUserContext, string globalFilePath)
    {
        _currentUserContext = currentUserContext;
        _globalFilePath = globalFilePath;
        _baseDirectory = Path.GetDirectoryName(globalFilePath) ?? AppPaths.BaseDir;
        _fileName = Path.GetFileName(globalFilePath);
    }

    public async Task<string> ResolveAsync()
    {
        if (_currentUserContext == null)
            return _globalFilePath;

        Guid userId;
        try
        {
            userId = await _currentUserContext.GetRequiredCurrentUserIdAsync().ConfigureAwait(false);
        }
        catch (InvalidOperationException)
        {
            return _globalFilePath;
        }

        return Path.Combine(_baseDirectory, "users", userId.ToString("N"), _fileName);
    }

    public async Task MigrateGlobalFileIfNeededAsync()
    {
        if (_currentUserContext == null || !File.Exists(_globalFilePath))
            return;

        var targetPath = await ResolveAsync().ConfigureAwait(false);
        if (File.Exists(targetPath))
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(_globalFilePath, targetPath, overwrite: false);
    }
}
