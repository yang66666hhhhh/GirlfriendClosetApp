namespace ClosetApp.Domain.Entities;

public class OutfitWornRecord : BaseEntity
{
    public Guid? OutfitId { get; set; }
    public Outfit? Outfit { get; set; }
    public DateTime WornDate { get; set; }
    public string OutfitNameSnapshot { get; set; } = string.Empty;
    public string? PreviewSnapshotPath { get; set; }
}
