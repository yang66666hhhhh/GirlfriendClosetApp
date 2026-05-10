using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace ClosetApp.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ClosetDbContext>
{
    public ClosetDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClosetApp",
            "closet.db");

        var optionsBuilder = new DbContextOptionsBuilder<ClosetDbContext>();
        optionsBuilder.UseSqlite($"Data Source={dbPath}");

        return new ClosetDbContext();
    }
}