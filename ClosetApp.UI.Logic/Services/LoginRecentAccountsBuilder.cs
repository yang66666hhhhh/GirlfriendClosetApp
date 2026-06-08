using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Logic.Services;

public sealed record LoginRecentAccountItem(
    Guid UserId,
    string AccountName,
    string DisplayName,
    string Initial,
    bool HasPinCredential);

public sealed record LoginRecentAccountsState(
    string? PrefillAccountName,
    IReadOnlyList<LoginRecentAccountItem> RecentAccounts)
{
    public bool HasRecentAccounts => RecentAccounts.Count > 0;
}

public static class LoginRecentAccountsBuilder
{
    private const int MaxRecentAccounts = 4;

    public static LoginRecentAccountsState Build(IEnumerable<LocalUser> users)
    {
        var recentAccounts = users
            .Where(user => user.IsActive && user.LastLoginAt.HasValue && !string.IsNullOrWhiteSpace(user.AccountName))
            .OrderByDescending(user => user.LastLoginAt)
            .Take(MaxRecentAccounts)
            .Select(user => new LoginRecentAccountItem(
                user.Id,
                user.AccountName.Trim(),
                string.IsNullOrWhiteSpace(user.DisplayName) ? user.AccountName.Trim() : user.DisplayName.Trim(),
                BuildInitial(user.DisplayName, user.AccountName),
                user.HasPinCredential))
            .ToList();

        return new LoginRecentAccountsState(
            recentAccounts.FirstOrDefault()?.AccountName,
            recentAccounts);
    }

    private static string BuildInitial(string? displayName, string? accountName)
    {
        var source = string.IsNullOrWhiteSpace(displayName) ? accountName : displayName;
        return string.IsNullOrWhiteSpace(source)
            ? "衣"
            : source.Trim()[0].ToString();
    }
}
