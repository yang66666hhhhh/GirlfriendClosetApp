using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#667eea";
    public TagCategory Category { get; set; } = TagCategory.Style;

    public ICollection<ClothingTag> ClothingTags { get; set; } = new List<ClothingTag>();
}
