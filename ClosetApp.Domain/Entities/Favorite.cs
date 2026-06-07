namespace ClosetApp.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid? LocalUserId { get; set; }
    public LocalUser? LocalUser { get; set; }
    public Guid OutfitId { get; set; }
    public Outfit Outfit { get; set; } = null!;
}
