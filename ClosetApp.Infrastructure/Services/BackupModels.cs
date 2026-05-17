using ClosetApp.Domain.Entities;

namespace ClosetApp.Infrastructure.Services;

internal sealed class ClosetBackupDocument
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; } = DateTime.Now;
    public List<Tag> Tags { get; set; } = [];
    public List<ClothingBackupItem> Clothes { get; set; } = [];
    public List<OutfitBackupItem> Outfits { get; set; } = [];
    public List<OutfitWornRecord> WornRecords { get; set; } = [];
    public List<Favorite> Favorites { get; set; } = [];
}

internal sealed class ClothingBackupItem : Clothing
{
    public List<Guid> TagIds { get; set; } = [];
}

internal sealed class OutfitBackupItem : Outfit
{
    public List<Guid> ClothingIds { get; set; } = [];
}
