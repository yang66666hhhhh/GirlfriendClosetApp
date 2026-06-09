using System.Security.Cryptography;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class LocalAuthService : ILocalAuthService
{
    private const int DefaultIterations = 120_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private readonly ILocalUserRepository _repository;
    private readonly ICurrentUserContext _currentUserContext;
    private readonly IAuthSessionContext _authSessionContext;

    public LocalAuthService(
        ILocalUserRepository repository,
        ICurrentUserContext currentUserContext,
        IAuthSessionContext authSessionContext)
    {
        _repository = repository;
        _currentUserContext = currentUserContext;
        _authSessionContext = authSessionContext;
    }

    public async Task<bool> HasAnyCredentialAsync()
    {
        var users = await _repository.GetAllAsync().ConfigureAwait(false);
        return users.Any(user => user.HasPasswordCredential);
    }

    public async Task<bool> HasCredentialAsync(Guid userId)
    {
        var user = await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false);
        return user?.HasPasswordCredential == true;
    }

    public async Task SetPasswordAsync(Guid userId, string password)
    {
        var user = await GetActiveUserAsync(userId).ConfigureAwait(false);
        ApplyPassword(user, password);
        await _repository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task SetPinAsync(Guid userId, string? pin)
    {
        var user = await GetActiveUserAsync(userId).ConfigureAwait(false);
        ApplyPin(user, pin);
        await _repository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task<LocalLoginResult> LoginAsync(Guid userId, string secret, LocalCredentialKind kind)
    {
        var user = await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
            return LocalLoginResult.Failed("用户不存在或已被删除。");

        return await LoginUserAsync(user, secret, kind).ConfigureAwait(false);
    }

    public async Task<LocalLoginResult> LoginAsync(string accountName, string secret, LocalCredentialKind kind)
    {
        string normalizedAccountName;
        try
        {
            normalizedAccountName = LocalUserService.NormalizeAccountName(accountName);
        }
        catch
        {
            return LocalLoginResult.Failed("账号或密码不正确。");
        }

        var user = await _repository.GetActiveByAccountNameAsync(normalizedAccountName).ConfigureAwait(false);
        if (user == null)
            return LocalLoginResult.Failed("账号或密码不正确。");

        return await LoginUserAsync(user, secret, kind).ConfigureAwait(false);
    }

    private async Task<LocalLoginResult> LoginUserAsync(LocalUser user, string secret, LocalCredentialKind kind)
    {
        var valid = kind == LocalCredentialKind.Password
            ? Verify(secret, user.PasswordHash, user.PasswordSalt, user.PasswordIterations)
            : Verify(secret, user.PinHash, user.PinSalt, user.PinIterations);

        if (!valid)
            return LocalLoginResult.Failed(kind == LocalCredentialKind.Pin ? "PIN 不正确。" : "密码不正确。");

        user.LastLoginAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
        await _repository.UpdateAsync(user).ConfigureAwait(false);
        _authSessionContext.MarkAuthenticated(user.Id);
        await _currentUserContext.SetCurrentUserIdAsync(user.Id).ConfigureAwait(false);
        return LocalLoginResult.Ok(user);
    }

    public Task LogoutAsync()
    {
        _authSessionContext.Clear();
        return Task.CompletedTask;
    }

    public async Task ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
    {
        var user = await GetActiveUserAsync(userId).ConfigureAwait(false);
        if (!Verify(oldPassword, user.PasswordHash, user.PasswordSalt, user.PasswordIterations))
            throw new InvalidOperationException("当前密码不正确。");

        ApplyPassword(user, newPassword);
        await _repository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task UpdateOwnCredentialAsync(Guid userId, string newPassword, string? newPin = null)
    {
        var user = await GetActiveUserAsync(userId).ConfigureAwait(false);
        ApplyPassword(user, newPassword);
        ApplyPin(user, newPin);
        await _repository.UpdateAsync(user).ConfigureAwait(false);
    }

    public async Task ResetMemberCredentialAsync(Guid userId, string newPassword, string? newPin = null)
    {
        var user = await GetActiveUserAsync(userId).ConfigureAwait(false);
        if (user.Role == LocalUserRole.SuperAdmin)
            throw new InvalidOperationException("不能通过成员重置入口修改超级管理员凭证。");

        ApplyPassword(user, newPassword);
        ApplyPin(user, newPin);
        await _repository.UpdateAsync(user).ConfigureAwait(false);
    }

    public static void ApplyPassword(LocalUser user, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new InvalidOperationException("密码至少需要 6 位。");

        var credential = CreateCredential(password);
        user.PasswordHash = credential.Hash;
        user.PasswordSalt = credential.Salt;
        user.PasswordIterations = credential.Iterations;
        user.CredentialUpdatedAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
    }

    public static void ApplyPin(LocalUser user, string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            user.PinHash = null;
            user.PinSalt = null;
            user.PinIterations = 0;
            user.CredentialUpdatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            return;
        }

        if (pin.Length < 4 || pin.Any(ch => !char.IsDigit(ch)))
            throw new InvalidOperationException("PIN 需要至少 4 位数字。");

        var credential = CreateCredential(pin);
        user.PinHash = credential.Hash;
        user.PinSalt = credential.Salt;
        user.PinIterations = credential.Iterations;
        user.CredentialUpdatedAt = DateTime.Now;
        user.UpdatedAt = DateTime.Now;
    }

    private async Task<LocalUser> GetActiveUserAsync(Guid userId)
    {
        return await _repository.GetActiveByIdAsync(userId).ConfigureAwait(false)
            ?? throw new InvalidOperationException("用户不存在或已被删除。");
    }

    private static (string Hash, string Salt, int Iterations) CreateCredential(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(secret, salt, DefaultIterations, HashAlgorithmName.SHA256, HashBytes);
        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt), DefaultIterations);
    }

    private static bool Verify(string secret, string? storedHash, string? storedSalt, int iterations)
    {
        if (string.IsNullOrWhiteSpace(secret) ||
            string.IsNullOrWhiteSpace(storedHash) ||
            string.IsNullOrWhiteSpace(storedSalt) ||
            iterations <= 0)
            return false;

        var salt = Convert.FromBase64String(storedSalt);
        var expected = Convert.FromBase64String(storedHash);
        var actual = Rfc2898DeriveBytes.Pbkdf2(secret, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
