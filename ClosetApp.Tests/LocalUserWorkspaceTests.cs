using System.IO;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Repositories;
using ClosetApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClosetApp.Tests;

public class LocalUserWorkspaceTests
{
    [Fact]
    public async Task EnsureInitializedAsync_WithEmptyDatabase_CreatesSuperAdmin()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            var service = new LocalUserService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json")));
            var user = await service.EnsureInitializedAsync();

            Assert.Equal(LocalUserRole.SuperAdmin, user.Role);
            Assert.Equal("admin", user.AccountName);
            Assert.True(user.IsActive);
            Assert.Equal(user.Id, await context.LocalUsers.Select(localUser => localUser.Id).SingleAsync());
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task EnsureInitializedAsync_AssignsUnownedLegacyDataToSuperAdmin()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.Clothes.Add(new Clothing { Name = "Legacy Coat", Type = ClothingType.Outerwear, Season = Season.Winter });
                setupContext.Tags.Add(new Tag { Name = "Legacy Tag", Category = TagCategory.Style });
                await setupContext.SaveChangesAsync();
            }

            await using (var initContext = new ClosetDbContext(options))
            {
                var service = new LocalUserService(new LocalUserRepository(initContext), new CurrentUserContext(Path.Combine(tempDir, "current-user.json")));
                await service.EnsureInitializedAsync();
            }

            await using var assertContext = new ClosetDbContext(options);
            var admin = Assert.Single(await assertContext.LocalUsers.ToListAsync());
            Assert.Equal(admin.Id, Assert.Single(await assertContext.Clothes.ToListAsync()).LocalUserId);
            Assert.Equal(admin.Id, Assert.Single(await assertContext.Tags.ToListAsync()).LocalUserId);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task EnsureInitializedAsync_WithDuplicateSuperAdmins_KeepsOnlyOldestAsSuperAdmin()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var firstAdminId = Guid.NewGuid();
        var duplicateAdminId = Guid.NewGuid();

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.LocalUsers.AddRange(
                    new LocalUser
                    {
                        Id = firstAdminId,
                        DisplayName = "私人衣橱",
                        Role = LocalUserRole.SuperAdmin,
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 1, 1)
                    },
                    new LocalUser
                    {
                        Id = duplicateAdminId,
                        DisplayName = "私人衣橱",
                        Role = LocalUserRole.SuperAdmin,
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 1, 2)
                    });
                await setupContext.SaveChangesAsync();
            }

            await using (var initContext = new ClosetDbContext(options))
            {
                var service = new LocalUserService(new LocalUserRepository(initContext), new CurrentUserContext(Path.Combine(tempDir, "current-user.json")));
                var admin = await service.EnsureInitializedAsync();

                Assert.Equal(firstAdminId, admin.Id);
            }

            await using var assertContext = new ClosetDbContext(options);
            var users = await assertContext.LocalUsers.OrderBy(user => user.CreatedAt).ToListAsync();
            Assert.Equal(LocalUserRole.SuperAdmin, users[0].Role);
            Assert.Equal(LocalUserRole.Member, users[1].Role);
            Assert.Equal("私人衣橱（成员）", users[1].DisplayName);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }


    [Fact]
    public async Task ClothingRepository_GetAllAsync_ReturnsOnlyCurrentUserClothes()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.LocalUsers.AddRange(
                    new LocalUser { Id = userA, DisplayName = "A", Role = LocalUserRole.SuperAdmin, IsActive = true },
                    new LocalUser { Id = userB, DisplayName = "B", Role = LocalUserRole.Member, IsActive = true });
                setupContext.Clothes.AddRange(
                    new Clothing { Name = "A Coat", LocalUserId = userA, Type = ClothingType.Outerwear, Season = Season.Winter },
                    new Clothing { Name = "B Coat", LocalUserId = userB, Type = ClothingType.Outerwear, Season = Season.Winter });
                await setupContext.SaveChangesAsync();
            }

            await using var queryContext = new ClosetDbContext(options);
            var currentUser = new CurrentUserContext(Path.Combine(tempDir, "current-user.json"));
            await currentUser.SetCurrentUserIdAsync(userA);
            var repository = new ClothingRepository(queryContext, currentUser);

            var clothes = (await repository.GetAllAsync()).ToList();

            var clothing = Assert.Single(clothes);
            Assert.Equal("A Coat", clothing.Name);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task DeleteAsync_ForMember_RemovesOnlyThatUserWorkspace()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.LocalUsers.AddRange(
                    new LocalUser { Id = adminId, DisplayName = "Admin", Role = LocalUserRole.SuperAdmin, IsActive = true },
                    new LocalUser { Id = memberId, DisplayName = "Member", Role = LocalUserRole.Member, IsActive = true });
                setupContext.Clothes.AddRange(
                    new Clothing { Name = "Admin Coat", LocalUserId = adminId, Type = ClothingType.Outerwear, Season = Season.Winter },
                    new Clothing { Name = "Member Coat", LocalUserId = memberId, Type = ClothingType.Outerwear, Season = Season.Winter });
                setupContext.Tags.AddRange(
                    new Tag { Name = "Admin Tag", LocalUserId = adminId, Category = TagCategory.Style },
                    new Tag { Name = "Member Tag", LocalUserId = memberId, Category = TagCategory.Style });
                await setupContext.SaveChangesAsync();
            }

            await using (var deleteContext = new ClosetDbContext(options))
            {
                var service = new LocalUserService(new LocalUserRepository(deleteContext), new CurrentUserContext(Path.Combine(tempDir, "current-user.json")));
                await service.DeleteAsync(memberId);
            }

            await using var assertContext = new ClosetDbContext(options);
            Assert.DoesNotContain(await assertContext.LocalUsers.ToListAsync(), user => user.Id == memberId);
            Assert.Contains(await assertContext.LocalUsers.ToListAsync(), user => user.Id == adminId);
            Assert.Contains(await assertContext.Clothes.ToListAsync(), clothing => clothing.LocalUserId == adminId);
            Assert.DoesNotContain(await assertContext.Clothes.ToListAsync(), clothing => clothing.LocalUserId == memberId);
            Assert.Contains(await assertContext.Tags.ToListAsync(), tag => tag.LocalUserId == adminId);
            Assert.DoesNotContain(await assertContext.Tags.ToListAsync(), tag => tag.LocalUserId == memberId);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task ScopedRepositories_ReturnOnlyCurrentUserData()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var outfitA = Guid.NewGuid();
        var outfitB = Guid.NewGuid();

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.LocalUsers.AddRange(
                    new LocalUser { Id = userA, DisplayName = "A", Role = LocalUserRole.SuperAdmin, IsActive = true },
                    new LocalUser { Id = userB, DisplayName = "B", Role = LocalUserRole.Member, IsActive = true });
                setupContext.Outfits.AddRange(
                    new Outfit { Id = outfitA, Name = "A Outfit", LocalUserId = userA, Scene = OutfitScene.Casual, Season = Season.AllSeason },
                    new Outfit { Id = outfitB, Name = "B Outfit", LocalUserId = userB, Scene = OutfitScene.Casual, Season = Season.AllSeason });
                setupContext.Tags.AddRange(
                    new Tag { Name = "A Tag", LocalUserId = userA, Category = TagCategory.Style },
                    new Tag { Name = "B Tag", LocalUserId = userB, Category = TagCategory.Style });
                setupContext.OutfitWornRecords.AddRange(
                    new OutfitWornRecord { OutfitId = outfitA, LocalUserId = userA, OutfitNameSnapshot = "A Outfit", WornDate = DateTime.Today },
                    new OutfitWornRecord { OutfitId = outfitB, LocalUserId = userB, OutfitNameSnapshot = "B Outfit", WornDate = DateTime.Today });
                setupContext.OutfitGeneratedImages.AddRange(
                    new OutfitGeneratedImage { OutfitId = outfitA, LocalUserId = userA, Status = "Succeeded" },
                    new OutfitGeneratedImage { OutfitId = outfitB, LocalUserId = userB, Status = "Succeeded" });
                await setupContext.SaveChangesAsync();
            }

            await using var queryContext = new ClosetDbContext(options);
            var currentUser = new CurrentUserContext(Path.Combine(tempDir, "current-user.json"));
            await currentUser.SetCurrentUserIdAsync(userA);

            Assert.Equal("A Outfit", Assert.Single(await new OutfitRepository(queryContext, currentUser).GetAllAsync()).Name);
            Assert.Equal("A Tag", Assert.Single(await new TagRepository(queryContext, currentUser).GetAllAsync()).Name);
            Assert.Equal("A Outfit", Assert.Single(await new OutfitWornRecordRepository(queryContext, currentUser).GetAllAsync()).OutfitNameSnapshot);
            Assert.Equal(outfitA, Assert.Single(await new OutfitGeneratedImageRepository(queryContext, currentUser).GetAllAsync()).OutfitId);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task PersonalProfileService_UsesCurrentUserProfile()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await setupContext.Database.EnsureCreatedAsync();
                setupContext.LocalUsers.AddRange(
                    new LocalUser { Id = userA, DisplayName = "A", Role = LocalUserRole.SuperAdmin, IsActive = true },
                    new LocalUser { Id = userB, DisplayName = "B", Role = LocalUserRole.Member, IsActive = true });
                setupContext.PersonalProfiles.AddRange(
                    new PersonalProfile { LocalUserId = userA, DisplayName = "Admin Profile" },
                    new PersonalProfile { LocalUserId = userB, DisplayName = "Member Profile" });
                await setupContext.SaveChangesAsync();
            }

            await using var queryContext = new ClosetDbContext(options);
            var currentUser = new CurrentUserContext(Path.Combine(tempDir, "current-user.json"));
            await currentUser.SetCurrentUserIdAsync(userB);
            var service = new PersonalProfileService(new PersonalProfileRepository(queryContext, currentUser), new FakeAiAssetStorageService());

            var profile = await service.GetCurrentAsync();

            Assert.NotNull(profile);
            Assert.Equal("Member Profile", profile.DisplayName);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalAuthService_SetPasswordAndLogin_AuthenticatesUser()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userId = Guid.NewGuid();

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            context.LocalUsers.Add(new LocalUser { Id = userId, DisplayName = "Admin", Role = LocalUserRole.SuperAdmin, IsActive = true });
            await context.SaveChangesAsync();

            var session = new AuthSessionContext();
            var auth = new LocalAuthService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session), session);

            Assert.False(await auth.HasAnyCredentialAsync());

            await auth.SetPasswordAsync(userId, "secret123");
            var failed = await auth.LoginAsync(userId, "wrong123", LocalCredentialKind.Password);
            var succeeded = await auth.LoginAsync(userId, "secret123", LocalCredentialKind.Password);

            Assert.False(failed.Success);
            Assert.True(succeeded.Success);
            Assert.True(session.IsAuthenticated);
            Assert.Equal(userId, session.AuthenticatedUserId);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalAuthService_LoginAsync_WithAccountName_AuthenticatesMatchingUser()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var adminId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            var admin = new LocalUser { Id = adminId, AccountName = "admin", DisplayName = "Admin", Role = LocalUserRole.SuperAdmin, IsActive = true };
            var member = new LocalUser { Id = memberId, AccountName = "xiaoyu", DisplayName = "小鱼", Role = LocalUserRole.Member, IsActive = true };
            LocalAuthService.ApplyPassword(admin, "admin123");
            LocalAuthService.ApplyPassword(member, "member123");
            context.LocalUsers.AddRange(admin, member);
            await context.SaveChangesAsync();

            var session = new AuthSessionContext();
            var auth = new LocalAuthService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session), session);

            var failed = await auth.LoginAsync("xiaoyu", "admin123", LocalCredentialKind.Password);
            var succeeded = await auth.LoginAsync(" xiaoyu ", "member123", LocalCredentialKind.Password);

            Assert.False(failed.Success);
            Assert.True(succeeded.Success);
            Assert.Equal(memberId, session.AuthenticatedUserId);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalAuthService_LoginAsync_UpdatesLastLoginTimestamp()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userId = Guid.NewGuid();

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            var user = new LocalUser
            {
                Id = userId,
                AccountName = "xiaoyu",
                DisplayName = "小鱼",
                Role = LocalUserRole.Member,
                IsActive = true
            };
            LocalAuthService.ApplyPassword(user, "member123");
            context.LocalUsers.Add(user);
            await context.SaveChangesAsync();

            var session = new AuthSessionContext();
            var auth = new LocalAuthService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session), session);

            var result = await auth.LoginAsync("xiaoyu", "member123", LocalCredentialKind.Password);

            Assert.True(result.Success);
            var refreshed = await context.LocalUsers.AsNoTracking().SingleAsync(item => item.Id == userId);
            Assert.NotNull(refreshed.LastLoginAt);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalUserService_CreateMemberAsync_RequiresUniqueAccountName()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            context.LocalUsers.Add(new LocalUser { Id = Guid.NewGuid(), AccountName = "admin", DisplayName = "Admin", Role = LocalUserRole.SuperAdmin, IsActive = true });
            await context.SaveChangesAsync();

            var service = new LocalUserService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json")));
            var user = await service.CreateMemberAsync("xiaoyu", "小鱼", "member123");

            Assert.Equal("xiaoyu", user.AccountName);
            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateMemberAsync(" XiaoYu ", "另一个小鱼", "member123"));
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalAuthService_PinIsOptionalAndCanAuthenticateWhenSet()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userId = Guid.NewGuid();

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            context.LocalUsers.Add(new LocalUser { Id = userId, DisplayName = "Member", Role = LocalUserRole.Member, IsActive = true });
            await context.SaveChangesAsync();

            var session = new AuthSessionContext();
            var auth = new LocalAuthService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session), session);

            await auth.SetPasswordAsync(userId, "secret123");
            Assert.False((await auth.LoginAsync(userId, "1234", LocalCredentialKind.Pin)).Success);

            await auth.SetPinAsync(userId, "1234");
            Assert.True((await auth.LoginAsync(userId, "1234", LocalCredentialKind.Pin)).Success);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalAuthService_ResetMemberCredential_InvalidatesOldPassword()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userId = Guid.NewGuid();

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            context.LocalUsers.Add(new LocalUser { Id = userId, DisplayName = "Member", Role = LocalUserRole.Member, IsActive = true });
            await context.SaveChangesAsync();

            var session = new AuthSessionContext();
            var auth = new LocalAuthService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session), session);

            await auth.SetPasswordAsync(userId, "secret123");
            await auth.ResetMemberCredentialAsync(userId, "newsecret123", "6789");

            Assert.False((await auth.LoginAsync(userId, "secret123", LocalCredentialKind.Password)).Success);
            Assert.True((await auth.LoginAsync(userId, "newsecret123", LocalCredentialKind.Password)).Success);
            Assert.True((await auth.LoginAsync(userId, "6789", LocalCredentialKind.Pin)).Success);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task LocalAuthService_UpdateOwnCredential_AllowsSuperAdminToChangePasswordAndPin()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);
        var userId = Guid.NewGuid();

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
            context.LocalUsers.Add(new LocalUser { Id = userId, DisplayName = "Admin", Role = LocalUserRole.SuperAdmin, IsActive = true });
            await context.SaveChangesAsync();

            var session = new AuthSessionContext();
            var auth = new LocalAuthService(new LocalUserRepository(context), new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session), session);

            await auth.SetPasswordAsync(userId, "secret123");
            await auth.UpdateOwnCredentialAsync(userId, "newsecret123", "2468");

            Assert.False((await auth.LoginAsync(userId, "secret123", LocalCredentialKind.Password)).Success);
            Assert.True((await auth.LoginAsync(userId, "newsecret123", LocalCredentialKind.Password)).Success);
            Assert.True((await auth.LoginAsync(userId, "2468", LocalCredentialKind.Pin)).Success);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task CurrentUserContext_GetRequiredCurrentUserIdAsync_RequiresAuthenticatedSession()
    {
        var tempDir = CreateTempDir();
        var userId = Guid.NewGuid();

        try
        {
            var session = new AuthSessionContext();
            var currentUser = new CurrentUserContext(Path.Combine(tempDir, "current-user.json"), session);
            await currentUser.SetCurrentUserIdAsync(userId);

            await Assert.ThrowsAsync<InvalidOperationException>(() => currentUser.GetRequiredCurrentUserIdAsync());

            session.MarkAuthenticated(userId);
            Assert.Equal(userId, await currentUser.GetRequiredCurrentUserIdAsync());

            await new LocalAuthService(new FakeLocalUserRepository(), currentUser, session).LogoutAsync();
            Assert.False(session.IsAuthenticated);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    private static DbContextOptions<ClosetDbContext> CreateOptions(string dbPath)
    {
        return new DbContextOptionsBuilder<ClosetDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch
        {
        }
    }

    private sealed class FakeAiAssetStorageService : ClosetApp.Application.Interfaces.IAiAssetStorageService
    {
        public Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName) => Task.FromResult(sourcePath);
        public Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType) => Task.FromResult("generated.png");
        public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName) => Task.CompletedTask;
        public Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName) => Task.CompletedTask;
        public Task TryDeleteProfileReferenceImageAsync(string? imagePath) => Task.CompletedTask;
        public Task TryDeleteGeneratedImageAsync(string? imagePath) => Task.CompletedTask;
        public string GetProfileReferenceFullPath(string relativePath) => relativePath;
        public string GetGeneratedImageFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath) => [relativePath];
    }

    private sealed class FakeLocalUserRepository : ClosetApp.Domain.Interfaces.ILocalUserRepository
    {
        public Task<IEnumerable<LocalUser>> GetAllAsync() => Task.FromResult<IEnumerable<LocalUser>>([]);
        public Task<LocalUser?> GetByIdAsync(Guid id) => Task.FromResult<LocalUser?>(null);
        public Task<LocalUser?> GetSuperAdminAsync() => Task.FromResult<LocalUser?>(null);
        public Task<LocalUser?> GetActiveByIdAsync(Guid id) => Task.FromResult<LocalUser?>(null);
        public Task<LocalUser?> GetActiveByAccountNameAsync(string accountName) => Task.FromResult<LocalUser?>(null);
        public Task<int> CountActiveAsync() => Task.FromResult(0);
        public Task AddAsync(LocalUser entity) => Task.CompletedTask;
        public Task UpdateAsync(LocalUser entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task AssignUnownedWorkspaceAsync(Guid userId) => Task.CompletedTask;
        public Task EnsureDefaultTagsAsync(Guid userId) => Task.CompletedTask;
        public Task DeleteWorkspaceAsync(Guid userId) => Task.CompletedTask;
    }
}
