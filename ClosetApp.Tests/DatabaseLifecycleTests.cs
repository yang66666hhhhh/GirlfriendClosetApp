using System.IO;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Infrastructure.Data;
using ClosetApp.Infrastructure.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClosetApp.Tests;

public class DatabaseLifecycleTests
{
    [Fact]
    public async Task InitializeAsync_WithFreshDatabase_CreatesSchemaAndHistory()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var options = CreateOptions(dbPath);

        try
        {
            await using var context = new ClosetDbContext(options);
            await context.Database.EnsureDeletedAsync();

            await ClosetDatabaseInitializer.InitializeAsync(context);

            Assert.True(await context.Database.CanConnectAsync());
            Assert.Equal(12, await context.Tags.CountAsync());
            Assert.True(await GetMigrationHistoryCountAsync(dbPath) > 0);
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithEnsureCreatedDatabase_BaselinesMigrationHistory()
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
                setupContext.Clothes.Add(new Clothing
                {
                    Name = "Legacy Coat",
                    Type = ClothingType.Outerwear,
                    Season = Season.Winter
                });
                await setupContext.SaveChangesAsync();
            }

            Assert.Equal(0, await GetMigrationHistoryCountAsync(dbPath));

            await using (var initContext = new ClosetDbContext(options))
            {
                await ClosetDatabaseInitializer.InitializeAsync(initContext);
            }

            await using var assertContext = new ClosetDbContext(options);
            Assert.True(await GetMigrationHistoryCountAsync(dbPath) > 0);
            Assert.Contains(await assertContext.Clothes.ToListAsync(), clothing => clothing.Name == "Legacy Coat");
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task InitializeAsync_WithLegacyCurrentSchemaMissingAccountName_AppliesLatestMigration()
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
                await setupContext.Database.ExecuteSqlRawAsync("DROP INDEX IF EXISTS IX_LocalUsers_AccountName;");
                await setupContext.Database.ExecuteSqlRawAsync("ALTER TABLE LocalUsers DROP COLUMN AccountName;");
            }

            await using (var initContext = new ClosetDbContext(options))
            {
                await ClosetDatabaseInitializer.InitializeAsync(initContext);
            }

            await using var assertContext = new ClosetDbContext(options);
            Assert.True(await ColumnExistsAsync(dbPath, "LocalUsers", "AccountName"));
        }
        finally
        {
            TryDeleteDirectory(tempDir);
        }
    }

    [Fact]
    public async Task InitializeAsync_AfterBackupRestore_KeepsImportedDataAccessible()
    {
        var tempDir = CreateTempDir();
        var dbPath = Path.Combine(tempDir, "closet.db");
        var historyDir = Path.Combine(tempDir, "history");
        var backupPath = Path.Combine(tempDir, "backup.json");
        var options = CreateOptions(dbPath);

        try
        {
            await using (var setupContext = new ClosetDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();
                await ClosetDatabaseInitializer.InitializeAsync(setupContext);
                setupContext.Clothes.Add(new Clothing
                {
                    Name = "Restored Coat",
                    Type = ClothingType.Outerwear,
                    Season = Season.Autumn,
                    FavoriteLevel = 4
                });
                await setupContext.SaveChangesAsync();
            }

            var backupService = new BackupService(new TestDbContextFactory(options), historyDirectory: historyDir);
            await backupService.ExportAsync(backupPath);

            await using (var resetContext = new ClosetDbContext(options))
            {
                await resetContext.Database.EnsureDeletedAsync();
            }

            await using (var importContext = new ClosetDbContext(options))
            {
                await ClosetDatabaseInitializer.InitializeAsync(importContext);
            }

            await backupService.ImportAsync(backupPath);

            await using (var restartContext = new ClosetDbContext(options))
            {
                await ClosetDatabaseInitializer.InitializeAsync(restartContext);
                Assert.Contains(await restartContext.Clothes.ToListAsync(), clothing => clothing.Name == "Restored Coat");
            }

            Assert.True(await GetMigrationHistoryCountAsync(dbPath) > 0);
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

    private static async Task<int> GetMigrationHistoryCountAsync(string dbPath)
    {
        await using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        await using var existsCommand = connection.CreateCommand();
        existsCommand.CommandText = """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = '__EFMigrationsHistory';
            """;

        var exists = Convert.ToInt32(await existsCommand.ExecuteScalarAsync()) > 0;
        if (!exists)
            return 0;

        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """SELECT COUNT(1) FROM "__EFMigrationsHistory";""";
        return Convert.ToInt32(await countCommand.ExecuteScalarAsync());
    }

    private static async Task<bool> ColumnExistsAsync(string dbPath, string tableName, string columnName)
    {
        await using var connection = new SqliteConnection($"Data Source={dbPath}");
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM pragma_table_info($tableName)
            WHERE name = $columnName;
            """;
        command.Parameters.AddWithValue("$tableName", tableName);
        command.Parameters.AddWithValue("$columnName", columnName);
        return Convert.ToInt32(await command.ExecuteScalarAsync()) > 0;
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

    private sealed class TestDbContextFactory : IDbContextFactory<ClosetDbContext>
    {
        private readonly DbContextOptions<ClosetDbContext> _options;

        public TestDbContextFactory(DbContextOptions<ClosetDbContext> options)
        {
            _options = options;
        }

        public ClosetDbContext CreateDbContext() => new(_options);

        public ValueTask<ClosetDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(CreateDbContext());
    }
}
