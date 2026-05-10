namespace ClosetApp.Domain.Entities;

public class ClothingTag
{
    public Guid ClothingId { get; set; }
    public Clothing Clothing { get; set; } = null!;

    public Guid TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
