using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

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
        var styleTags = new[] {
            ("韩系", "#D8B7A3"),
            ("极简", "#C8C2B8"),
            ("通勤", "#B7C4B2"),
            ("甜妹", "#E7C7C0"),
            ("美式", "#C89B7B"),
            ("复古", "#C4A98F"),
            ("Clean Fit", "#A8B5A0"),
            ("Y2K", "#D4A5C9")
        };
        var sceneTags = new[] {
            ("约会", "#E88D8D"),
            ("上班", "#8BA8D9"),
            ("出游", "#A8D4A8"),
            ("派对", "#D4A5E8")
        };

        int id = 1;
        foreach (var (name, color) in styleTags)
            modelBuilder.Entity<Tag>().HasData(new Tag { Id = Guid.Parse($"00000000-0000-0000-0000-{id++:D12}"), Name = name, Color = color, Category = TagCategory.Style });
        foreach (var (name, color) in sceneTags)
            modelBuilder.Entity<Tag>().HasData(new Tag { Id = Guid.Parse($"00000000-0000-0000-0000-{id++:D12}"), Name = name, Color = color, Category = TagCategory.Scene });
    }
}