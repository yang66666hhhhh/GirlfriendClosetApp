namespace ClosetApp.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid OutfitId { get; set; }
    public Outfit Outfit { get; set; } = null!;
}
