using Microsoft.EntityFrameworkCore;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Infrastructure.Data;

public class ClosetDbContext : DbContext
{
    public DbSet<LocalUser> LocalUsers => Set<LocalUser>();
    public DbSet<Clothing> Clothes => Set<Clothing>();
    public DbSet<Outfit> Outfits => Set<Outfit>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ClothingTag> ClothingTags => Set<ClothingTag>();
    public DbSet<OutfitClothing> OutfitClothes => Set<OutfitClothing>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<OutfitWornRecord> OutfitWornRecords => Set<OutfitWornRecord>();
    public DbSet<PersonalProfile> PersonalProfiles => Set<PersonalProfile>();
    public DbSet<OutfitGeneratedImage> OutfitGeneratedImages => Set<OutfitGeneratedImage>();

    private readonly string _dbPath;

    public ClosetDbContext()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "ClosetApp");
        Directory.CreateDirectory(appFolder);
        _dbPath = Path.Combine(appFolder, "closet.db");
    }

    public ClosetDbContext(DbContextOptions<ClosetDbContext> options) : base(options)
    {
        _dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClosetApp",
            "closet.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseSqlite($"Data Source={_dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LocalUser>()
            .Property(user => user.Role)
            .HasConversion<string>();

        modelBuilder.Entity<LocalUser>()
            .Property(user => user.AccountName)
            .HasDefaultValue("admin");

        modelBuilder.Entity<LocalUser>()
            .HasIndex(user => user.AccountName)
            .HasDatabaseName("IX_LocalUsers_AccountName");

        modelBuilder.Entity<LocalUser>()
            .HasIndex(user => user.Role);

        modelBuilder.Entity<Clothing>()
            .HasOne(clothing => clothing.LocalUser)
            .WithMany(user => user.Clothes)
            .HasForeignKey(clothing => clothing.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Outfit>()
            .HasOne(outfit => outfit.LocalUser)
            .WithMany(user => user.Outfits)
            .HasForeignKey(outfit => outfit.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tag>()
            .HasOne(tag => tag.LocalUser)
            .WithMany(user => user.Tags)
            .HasForeignKey(tag => tag.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Favorite>()
            .HasOne(favorite => favorite.LocalUser)
            .WithMany(user => user.Favorites)
            .HasForeignKey(favorite => favorite.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OutfitWornRecord>()
            .HasOne(record => record.LocalUser)
            .WithMany(user => user.WornRecords)
            .HasForeignKey(record => record.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PersonalProfile>()
            .HasOne(profile => profile.LocalUser)
            .WithMany(user => user.PersonalProfiles)
            .HasForeignKey(profile => profile.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OutfitGeneratedImage>()
            .HasOne(image => image.LocalUser)
            .WithMany(user => user.OutfitGeneratedImages)
            .HasForeignKey(image => image.LocalUserId)
            .OnDelete(DeleteBehavior.Cascade);

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
            .HasForeignKey(r => r.OutfitId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<OutfitGeneratedImage>()
            .HasOne(image => image.Outfit)
            .WithMany(outfit => outfit.GeneratedImages)
            .HasForeignKey(image => image.OutfitId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
