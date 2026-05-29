using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Entities;

public class Outfit : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public OutfitScene Scene { get; set; }
    public Season Season { get; set; }
    public int Rating { get; set; } = 3;
    public string? Notes { get; set; }
    public DateTime? WornDate { get; set; }
    public int WearCount { get; set; }
    public int OriginalClothingCount { get; set; }

    public ICollection<OutfitClothing> OutfitClothes { get; set; } = new List<OutfitClothing>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<OutfitWornRecord> WornRecords { get; set; } = new List<OutfitWornRecord>();
}
