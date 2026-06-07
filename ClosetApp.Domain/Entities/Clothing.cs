using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Entities;

public class Clothing : BaseEntity
{
    public Guid? LocalUserId { get; set; }
    public LocalUser? LocalUser { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClothingType Type { get; set; }
    public GarmentType? GarmentType { get; set; }
    public string? ImagePath { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
    public Season Season { get; set; }
    public int FavoriteLevel { get; set; }

    public ICollection<OutfitClothing> OutfitClothes { get; set; } = new List<OutfitClothing>();
    public ICollection<ClothingTag> ClothingTags { get; set; } = new List<ClothingTag>();
}
