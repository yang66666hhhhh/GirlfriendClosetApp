namespace ClosetApp.Application.DTOs;

public class ClothingSnapshotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImagePath { get; set; }
    public string Type { get; set; } = string.Empty;
}
