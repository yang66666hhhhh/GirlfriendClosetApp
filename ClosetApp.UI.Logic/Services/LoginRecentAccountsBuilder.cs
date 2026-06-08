using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Logic.Services;

public sealed record LoginRecentAccountItem(
    Guid UserId,
    string AccountName,
    string DisplayName,
    string Initial,
    bool HasPinCredential,
    string LastLoginText);

public sealed record LoginRecentAccountsState(
    string? PrefillAccountName,
    IReadOnlyList<LoginRecentAccountItem> RecentAccounts)
{
    public bool HasRecentAccounts => RecentAccounts.Count > 0;
}

public static class LoginRecentAccountsBuilder
{
    private const int MaxRecentAccounts = 4;

    public static LoginRecentAccountsState Build(IEnumerable<LocalUser> users, DateTime? now = null)
    {
        var currentTime = now ?? DateTime.Now;
        var recentAccounts = users
            .Where(user => user.IsActive && user.LastLoginAt.HasValue && !string.IsNullOrWhiteSpace(user.AccountName))
            .OrderByDescending(user => user.LastLoginAt)
            .Take(MaxRecentAccounts)
            .Select(user => new LoginRecentAccountItem(
                user.Id,
                user.AccountName.Trim(),
                string.IsNullOrWhiteSpace(user.DisplayName) ? user.AccountName.Trim() : user.DisplayName.Trim(),
                BuildInitial(user.DisplayName, user.AccountName),
                user.HasPinCredential,
                FormatLastLoginText(user.LastLoginAt!.Value, currentTime)))
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

    private static string FormatLastLoginText(DateTime lastLoginAt, DateTime now)
    {
        var delta = now - lastLoginAt;
        if (delta.TotalMinutes < 1)
            return "刚刚";

        if (delta.TotalHours < 1)
            return $"{Math.Max(1, (int)Math.Floor(delta.TotalMinutes))} 分钟前";

        if (delta.TotalDays < 1)
            return $"{Math.Max(1, (int)Math.Floor(delta.TotalHours))} 小时前";

        if (delta.TotalDays < 2)
            return "昨天";

        return $"{Math.Max(2, (int)Math.Floor(delta.TotalDays))} 天前";
    }
}
