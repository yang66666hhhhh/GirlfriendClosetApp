using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Services;
using Xunit;

namespace ClosetApp.Tests;

public class LoginRecentAccountsBuilderTests
{
    [Fact]
    public void Build_WithRecentLogins_UsesMostRecentAccountAsPrefill()
    {
        var now = new DateTime(2026, 6, 8, 9, 30, 0);
        var users = new[]
        {
            new LocalUser
            {
                Id = Guid.NewGuid(),
                AccountName = "admin",
                DisplayName = "私人衣橱",
                Role = LocalUserRole.SuperAdmin,
                IsActive = true,
                LastLoginAt = now.AddHours(-5)
            },
            new LocalUser
            {
                Id = Guid.NewGuid(),
                AccountName = "xiaoyu",
                DisplayName = "小鱼",
                Role = LocalUserRole.Member,
                IsActive = true,
                LastLoginAt = now.AddMinutes(-15),
                PinHash = "pin",
                PinSalt = "salt"
            },
            new LocalUser
            {
                Id = Guid.NewGuid(),
                AccountName = "nana",
                DisplayName = "娜娜",
                Role = LocalUserRole.Member,
                IsActive = true,
                LastLoginAt = now.AddHours(-2)
            }
        };

        var state = LoginRecentAccountsBuilder.Build(users);

        Assert.Equal("xiaoyu", state.PrefillAccountName);
        Assert.Equal(3, state.RecentAccounts.Count);
        Assert.Equal("xiaoyu", state.RecentAccounts[0].AccountName);
        Assert.True(state.RecentAccounts[0].HasPinCredential);
        Assert.Equal("小", state.RecentAccounts[0].Initial);
    }

    [Fact]
    public void Build_WithoutRecentLogins_ReturnsEmptyState()
    {
        var users = new[]
        {
            new LocalUser
            {
                Id = Guid.NewGuid(),
                AccountName = "member",
                DisplayName = "成员",
                Role = LocalUserRole.Member,
                IsActive = true
            }
        };

        var state = LoginRecentAccountsBuilder.Build(users);

        Assert.Null(state.PrefillAccountName);
        Assert.Empty(state.RecentAccounts);
        Assert.False(state.HasRecentAccounts);
    }
}
