using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class LocalUserService : ILocalUserService
{
    public const string DefaultAdminAccountName = "admin";
    private const string DefaultAdminName = "私人衣橱";
    private readonly ILocalUserRepository _repository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAiAssetStorageService? _assetStorageService;

    public LocalUserService(
        ILocalUserRepository repository,
        ICurrentUserContext currentUserContext,
        IAiAssetStorageService? assetStorageService = null)
    {
        _repository = repository;
        _currentUserContext = currentUserContext;
        _assetStorageService = assetStorageService;
    }

    public async Task<LocalUser> EnsureInitializedAsync()
    {
        var superAdmin = await NormalizeSuperAdminsAsync().ConfigureAwait(false);
        if (superAdmin == null)
        {
            superAdmin = new LocalUser
            {
                Id = Guid.NewGuid(),
                AccountName = DefaultAdminAccountName,
                DisplayName = DefaultAdminName,
                Role = LocalUserRole.SuperAdmin,
                IsActive = true
            };
            await _repository.AddAsync(superAdmin).ConfigureAwait(false);
        }
        else
        {
            await EnsureAccountNamesAsync(superAdmin).ConfigureAwait(false);
            superAdmin = await _repository.GetActiveByIdAsync(superAdmin.Id).ConfigureAwait(false) ?? superAdmin;
        }

        var currentUserId = await _currentUserContext.GetCurrentUserIdAsync().ConfigureAwait(false);
        if (!currentUserId.HasValue || await _repository.GetActiveByIdAsync(currentUserId.Value).ConfigureAwait(false) == null)
            await _currentUserContext.SetCurrentUserIdAsync(superAdmin.Id).ConfigureAwait(false);

        await _repository.AssignUnownedWorkspaceAsync(superAdmin.Id).ConfigureAwait(false);
        await _repository.EnsureDefaultTagsAsync(superAdmin.Id).ConfigureAwait(false);

        return superAdmin;
    }

    private async Task EnsureAccountNamesAsync(LocalUser superAdmin)
    {
        var users = (await _repository.GetAllAsync().ConfigureAwait(false)).ToList();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in users.OrderBy(user => user.Role == LocalUserRole.SuperAdmin ? 0 : 1).ThenBy(user => user.CreatedAt))
        {
            var preferred = user.Id == superAdmin.Id
                ? DefaultAdminAccountName
                : string.IsNullOrWhiteSpace(user.AccountName) ? "user" : user.AccountName;
            var normalized = MakeUniqueAccountName(NormalizeAccountNameLenient(preferred), used);
            used.Add(normalized);

            if (user.AccountName == normalized)
                continue;

            user.AccountName = normalized;
            user.UpdatedAt = DateTime.Now;
            await _repository.UpdateAsync(user).ConfigureAwait(false);
        }
    }

    private async Task<LocalUser?> NormalizeSuperAdminsAsync()
    {
        var users = (await _repository.GetAllAsync().ConfigureAwait(false)).ToList();
        var superAdmins = users
            .Where(user => user.Role == LocalUserRole.SuperAdmin)
            .OrderBy(user => user.CreatedAt)
            .ToList();

        var keeper = superAdmins.FirstOrDefault();
        foreach (var duplicate in superAdmins.Skip(1))
        {
            duplicate.Role = LocalUserRole.Member;
            if (string.Equals(duplicate.DisplayName, keeper?.DisplayName, StringComparison.OrdinalIgnoreCase))
                duplicate.DisplayName = $"{duplicate.DisplayName}（成员）";
            duplicate.UpdatedAt = DateTime.Now;
            await _repository.UpdateAsync(duplicate).ConfigureAwait(false);
        }

        return keeper;
    }

    public async Task<IReadOnlyList<LocalUser>> GetAllAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        return (await _repository.GetAllAsync().ConfigureAwait(false)).ToList();
    }

    public async Task<LocalUser> GetCurrentAsync()
    {
        await EnsureInitializedAsync().ConfigureAwait(false);
        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync().ConfigureAwait(false);
        return await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false)
            ?? await EnsureInitializedAsync().ConfigureAwait(false);
    }

    public async Task<LocalUser> CreateMemberAsync(string displayName)
    {
        var accountName = await CreateUniqueGeneratedAccountNameAsync().ConfigureAwait(false);
        var user = new LocalUser
        {
            Id = Guid.NewGuid(),
            AccountName = accountName,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "新用户" : displayName.Trim(),
            Role = LocalUserRole.Member,
            IsActive = true
        };
        await _repository.AddAsync(user).ConfigureAwait(false);
        return user;
    }

    public async Task<LocalUser> CreateMemberAsync(string accountName, string displayName, string initialPassword, string? initialPin = null)
    {
        var normalizedAccountName = NormalizeAccountName(accountName);
        if (await _repository.GetActiveByAccountNameAsync(normalizedAccountName).ConfigureAwait(false) != null)
            throw new InvalidOperationException("账号已存在，请换一个。");

        var user = new LocalUser
        {
            Id = Guid.NewGuid(),
            AccountName = normalizedAccountName,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "新用户" : displayName.Trim(),
            Role = LocalUserRole.Member,
            IsActive = true
        };
        LocalAuthService.ApplyPassword(user, initialPassword);
        LocalAuthService.ApplyPin(user, initialPin);
        await _repository.AddAsync(user).ConfigureAwait(false);
        return user;
    }

    private async Task<string> CreateUniqueGeneratedAccountNameAsync()
    {
        var users = await _repository.GetAllAsync().ConfigureAwait(false);
        var used = users
            .Select(user => NormalizeAccountNameLenient(user.AccountName))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return MakeUniqueAccountName("user", used);
    }

    public static string NormalizeAccountName(string accountName)
    {
        var normalized = NormalizeAccountNameLenient(accountName);
        if (normalized.Length < 3)
            throw new InvalidOperationException("账号至少需要 3 位。");

        if (normalized.Any(ch => !char.IsAsciiLetterOrDigit(ch) && ch != '_' && ch != '-'))
            throw new InvalidOperationException("账号只能使用字母、数字、下划线或短横线。");

        return normalized;
    }

    private static string NormalizeAccountNameLenient(string? accountName)
    {
        var normalized = (accountName ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
            return "user";

        var chars = normalized
            .Select(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_' || ch == '-' ? ch : '-')
            .ToArray();
        normalized = new string(chars).Trim('-');
        return normalized.Length >= 3 ? normalized : "user";
    }

    private static string MakeUniqueAccountName(string preferred, HashSet<string> used)
    {
        if (!used.Contains(preferred))
            return preferred;

        for (var index = 2; ; index++)
        {
            var candidate = $"{preferred}{index}";
            if (!used.Contains(candidate))
                return candidate;
        }
    }

    public async Task<LocalUser> UpdateAsync(
        Guid userId,
        string displayName,
        string? avatarPhotoPath = null,
        string? accountName = null,
        string? avatarSourcePath = null,
        bool removeAvatarPhoto = false)
    {
        var user = await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("要编辑的用户不存在。");

        if (accountName != null)
        {
            var normalizedAccountName = NormalizeAccountName(accountName);
            var existing = await _repository.GetActiveByAccountNameAsync(normalizedAccountName).ConfigureAwait(false);
            if (existing != null && existing.Id != user.Id)
                throw new InvalidOperationException("账号已存在，请换一个。");

            user.AccountName = normalizedAccountName;
        }

        user.DisplayName = string.IsNullOrWhiteSpace(displayName) ? user.DisplayName : displayName.Trim();
        if (removeAvatarPhoto)
        {
            if (_assetStorageService != null)
                await _assetStorageService.TryDeleteProfileReferenceImageAsync(user.AvatarPhotoPath, user.Id).ConfigureAwait(false);
            user.AvatarPhotoPath = null;
        }
        else if (!string.IsNullOrWhiteSpace(avatarSourcePath) && _assetStorageService != null)
        {
            if (!string.IsNullOrWhiteSpace(user.AvatarPhotoPath))
                await _assetStorageService.TryDeleteProfileReferenceImageAsync(user.AvatarPhotoPath, user.Id).ConfigureAwait(false);

            var slotName = $"user-{user.Id:N}-avatar";
            user.AvatarPhotoPath = await _assetStorageService
                .SaveProfileReferenceImageAsync(avatarSourcePath, slotName, user.Id)
                .ConfigureAwait(false);
        }
        else if (avatarPhotoPath != null)
        {
            user.AvatarPhotoPath = avatarPhotoPath;
        }

        user.UpdatedAt = DateTime.Now;
        await _repository.UpdateAsync(user).ConfigureAwait(false);
        return user;
    }

    public async Task SwitchAsync(Guid userId)
    {
        var user = await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("要切换的用户不存在。");

        await _currentUserContext.SetCurrentUserIdAsync(user.Id).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid userId)
    {
        var user = await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("要删除的用户不存在。");

        if (user.Role == LocalUserRole.SuperAdmin)
            throw new InvalidOperationException("超级管理员账号不可删除。");

        await _repository.DeleteWorkspaceAsync(userId).ConfigureAwait(false);

        var currentUserId = await _currentUserContext.GetCurrentUserIdAsync().ConfigureAwait(false);
        if (currentUserId == userId)
        {
            var superAdmin = await EnsureInitializedAsync().ConfigureAwait(false);
            await _currentUserContext.SetCurrentUserIdAsync(superAdmin.Id).ConfigureAwait(false);
        }
    }
}
