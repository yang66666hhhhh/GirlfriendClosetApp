namespace ClosetApp.Domain.Entities;

public class OutfitWornRecord : BaseEntity
{
    public Guid OutfitId { get; set; }
    public Outfit Outfit { get; set; } = null!;
    public DateTime WornDate { get; set; }
}
