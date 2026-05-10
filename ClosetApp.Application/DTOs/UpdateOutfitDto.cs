using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public class UpdateOutfitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OutfitScene Scene { get; set; }
    public Season Season { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public List<Guid> ClothingIds { get; set; } = new();
}