using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public class CreateOutfitDto
{
    public string Name { get; set; } = string.Empty;
    public OutfitScene Scene { get; set; }
    public Season Season { get; set; }
    public int Rating { get; set; } = 3;
    public string? Notes { get; set; }
    public List<Guid> ClothingIds { get; set; } = new();
}