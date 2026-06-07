using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ClosetApp.Infrastructure.Data;

public static class ClosetDatabaseInitializer
{
    private const string EfCoreProductVersion = "8.0.0";
    private const string AddLocalUserAccountNameMigrationId = "20260607123000_AddLocalUserAccountName";

    public static async Task InitializeAsync(ClosetDbContext dbContext, CancellationToken cancellationToken = default)
    {
        if (await HasMigrationHistoryTableAsync(dbContext, cancellationToken))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        if (!await HasUserTablesAsync(dbContext, cancellationToken))
        {
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await EnsureMigrationHistoryTableAsync(dbContext, cancellationToken);
        var migrationIds = ResolveMigrationIds(dbContext).ToList();
        if (await HasCurrentModelSchemaAsync(dbContext, cancellationToken))
        {
            await InsertMigrationHistoryAsync(dbContext, migrationIds, cancellationToken);
            return;
        }

        if (await HasSchemaBeforeLocalUserAccountNameAsync(dbContext, cancellationToken))
        {
            await InsertMigrationHistoryAsync(
                dbContext,
                migrationIds.TakeWhile(id => !string.Equals(id, AddLocalUserAccountNameMigrationId, StringComparison.Ordinal)),
                cancellationToken);
            await dbContext.Database.MigrateAsync(cancellationToken);
            return;
        }

        await InsertMigrationHistoryAsync(dbContext, migrationIds.Take(1), cancellationToken);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<bool> HasMigrationHistoryTableAsync(ClosetDbContext dbContext, CancellationToken cancellationToken)
    {
        return await TableExistsAsync(dbContext, "__EFMigrationsHistory", cancellationToken);
    }

    private static async Task<bool> HasUserTablesAsync(ClosetDbContext dbContext, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name NOT LIKE 'sqlite_%'
              AND name <> '__EFMigrationsHistory';
            """;

        return await ExecuteScalarAsync(dbContext, sql, cancellationToken) > 0;
    }

    private static async Task<bool> TableExistsAsync(ClosetDbContext dbContext, string tableName, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = $name;
            """;

        return await ExecuteScalarAsync(dbContext, sql, cancellationToken, ("$name", tableName)) > 0;
    }

    private static async Task EnsureMigrationHistoryTableAsync(ClosetDbContext dbContext, CancellationToken cancellationToken)
    {
        const string sql = """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
                "ProductVersion" TEXT NOT NULL
            );
            """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task<bool> HasCurrentModelSchemaAsync(ClosetDbContext dbContext, CancellationToken cancellationToken)
    {
        return await HasSchemaBeforeLocalUserAccountNameAsync(dbContext, cancellationToken)
            && await ColumnExistsAsync(dbContext, "LocalUsers", "AccountName", cancellationToken);
    }

    private static async Task<bool> HasSchemaBeforeLocalUserAccountNameAsync(ClosetDbContext dbContext, CancellationToken cancellationToken)
    {
        return await ColumnExistsAsync(dbContext, "Outfits", "OriginalClothingCount", cancellationToken)
            && await ColumnExistsAsync(dbContext, "OutfitWornRecords", "OutfitNameSnapshot", cancellationToken)
            && await ColumnExistsAsync(dbContext, "OutfitWornRecords", "ClothingDetailsSnapshot", cancellationToken)
            && await ColumnExistsAsync(dbContext, "OutfitWornRecords", "IsSnapshotComplete", cancellationToken);
    }

    private static async Task<bool> ColumnExistsAsync(
        ClosetDbContext dbContext,
        string tableName,
        string columnName,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT COUNT(1)
            FROM pragma_table_info($tableName)
            WHERE name = $columnName;
            """;

        return await ExecuteScalarAsync(
            dbContext,
            sql,
            cancellationToken,
            ("$tableName", tableName),
            ("$columnName", columnName)) > 0;
    }

    private static async Task InsertMigrationHistoryAsync(
        ClosetDbContext dbContext,
        IEnumerable<string> migrationIds,
        CancellationToken cancellationToken)
    {
        foreach (var migrationId in migrationIds)
        {
            const string existsSql = """
                SELECT COUNT(1)
                FROM "__EFMigrationsHistory"
                WHERE "MigrationId" = $migrationId;
                """;

            var existingCount = await ExecuteScalarAsync(
                dbContext,
                existsSql,
                cancellationToken,
                ("$migrationId", migrationId));

            if (existingCount > 0)
                continue;

            const string insertSql = """
                INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
                VALUES ($migrationId, $productVersion);
                """;

            await ExecuteNonQueryAsync(
                dbContext,
                insertSql,
                cancellationToken,
                ("$migrationId", migrationId),
                ("$productVersion", EfCoreProductVersion));
        }
    }

    private static IEnumerable<string> ResolveMigrationIds(ClosetDbContext dbContext)
    {
        var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
        var migrationIds = migrationsAssembly.Migrations.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
        if (migrationIds.Count == 0)
            throw new InvalidOperationException("当前数据库项目没有可用的 EF 迁移，无法建立迁移历史。");

        return migrationIds;
    }

    private static async Task<long> ExecuteScalarAsync(
        ClosetDbContext dbContext,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static async Task ExecuteNonQueryAsync(
        ClosetDbContext dbContext,
        string sql,
        CancellationToken cancellationToken,
        params (string Name, object? Value)[] parameters)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = name;
                parameter.Value = value ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }
}
