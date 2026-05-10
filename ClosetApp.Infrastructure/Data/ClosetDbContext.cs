using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Infrastructure.Data;

public class ClosetDbContext : DbContext
{
    public DbSet<Clothing> Clothes => Set<Clothing>();
    public DbSet<Outfit> Outfits => Set<Outfit>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ClothingTag> ClothingTags => Set<ClothingTag>();
    public DbSet<OutfitClothing> OutfitClothes => Set<OutfitClothing>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<OutfitWornRecord> OutfitWornRecords => Set<OutfitWornRecord>();

    private readonly string _dbPath;

    public ClosetDbContext()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "ClosetApp");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "closet.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutfitClothing>()
            .HasKey(oc => new { oc.OutfitId, oc.ClothingId });

        modelBuilder.Entity<OutfitClothing>()
            .HasOne(oc => oc.Outfit)
            .WithMany(o => o.OutfitClothes)
            .HasForeignKey(oc => oc.OutfitId);

        modelBuilder.Entity<OutfitClothing>()
            .HasOne(oc => oc.Clothing)
            .WithMany(c => c.OutfitClothes)
            .HasForeignKey(oc => oc.ClothingId);

        modelBuilder.Entity<ClothingTag>()
            .HasKey(ct => new { ct.ClothingId, ct.TagId });

        modelBuilder.Entity<ClothingTag>()
            .HasOne(ct => ct.Clothing)
            .WithMany(c => c.ClothingTags)
            .HasForeignKey(ct => ct.ClothingId);

        modelBuilder.Entity<ClothingTag>()
            .HasOne(ct => ct.Tag)
            .WithMany(t => t.ClothingTags)
            .HasForeignKey(ct => ct.TagId);

        modelBuilder.Entity<Favorite>()
            .HasOne(f => f.Outfit)
            .WithMany(o => o.Favorites)
            .HasForeignKey(f => f.OutfitId);

        modelBuilder.Entity<OutfitWornRecord>()
            .HasOne(r => r.Outfit)
            .WithMany(o => o.WornRecords)
            .HasForeignKey(r => r.OutfitId);

        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        var styleTags = new[] { "韩系", "通勤", "可爱", "辣妹", "休闲" };
        var sceneTags = new[] { "约会", "上班", "出游", "派对" };
        var seasonTags = new[] { "春", "夏", "秋", "冬" };

        int id = 1;
        foreach (var name in styleTags)
            modelBuilder.Entity<Tag>().HasData(new Tag { Id = Guid.Parse($"00000000-0000-0000-0000-{id++:D12}"), Name = name, Color = "#667eea" });
        foreach (var name in sceneTags)
            modelBuilder.Entity<Tag>().HasData(new Tag { Id = Guid.Parse($"00000000-0000-0000-0000-{id++:D12}"), Name = name, Color = "#ec4899" });
        foreach (var name in seasonTags)
            modelBuilder.Entity<Tag>().HasData(new Tag { Id = Guid.Parse($"00000000-0000-0000-0000-{id++:D12}"), Name = name, Color = "#10b981" });
    }
}