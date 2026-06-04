namespace ClosetApp.Domain.Entities;

public class OutfitGeneratedImage : BaseEntity
{
    public Guid OutfitId { get; set; }
    public Outfit? Outfit { get; set; }
    public string ProviderKind { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string PromptSnapshot { get; set; } = string.Empty;
    public string ProfileSnapshotJson { get; set; } = string.Empty;
    public string OutfitSnapshotJson { get; set; } = string.Empty;
    public string OptionSnapshotJson { get; set; } = string.Empty;
    public string? ResultImagePath { get; set; }
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
}
