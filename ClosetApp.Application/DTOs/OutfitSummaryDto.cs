using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public class OutfitSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public OutfitScene Scene { get; set; }
    public Season Season { get; set; }
    public int Rating { get; set; }
    public int ClothingCount { get; set; }
    public string? PreviewImagePath { get; set; }
}