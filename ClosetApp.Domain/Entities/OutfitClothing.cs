namespace ClosetApp.Domain.Entities;

public class OutfitClothing
{
    public Guid OutfitId { get; set; }
    public Outfit Outfit { get; set; } = null!;

    public Guid ClothingId { get; set; }
    public Clothing Clothing { get; set; } = null!;
}
