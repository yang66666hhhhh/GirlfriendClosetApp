using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public class OutfitDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OutfitScene Scene { get; set; }
    public Season Season { get; set; }
    public int Rating { get; set; }
    public string? Notes { get; set; }
    public DateTime? WornDate { get; set; }
    public int WearCount { get; set; }
    public List<ClothingDto> Clothes { get; set; } = new();
}

public class ClothingDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ClothingType Type { get; set; }
    public string? ImagePath { get; set; }
}