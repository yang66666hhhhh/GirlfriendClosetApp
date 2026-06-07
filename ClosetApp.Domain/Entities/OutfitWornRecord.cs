namespace ClosetApp.Domain.Entities;

public class OutfitWornRecord : BaseEntity
{
    public Guid? LocalUserId { get; set; }
    public LocalUser? LocalUser { get; set; }
    public Guid? OutfitId { get; set; }
    public Outfit? Outfit { get; set; }
    public DateTime WornDate { get; set; }
    public string OutfitNameSnapshot { get; set; } = string.Empty;
    public string? PreviewSnapshotPath { get; set; }
    public string? OutfitClothingIdsSnapshot { get; set; }
    public int ClothingCountSnapshot { get; set; }
    public string? ClothingDetailsSnapshot { get; set; }
    public bool IsSnapshotComplete { get; set; }
}
