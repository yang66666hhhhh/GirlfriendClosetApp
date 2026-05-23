using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

// Carries ranking details so the UI can explain why an outfit surfaced today.
public sealed record RecommendedOutfitDto(
    Outfit Outfit,
    int Score,
    string PrimaryReason,
    string? SecondaryReason)
{
    public string Name => Outfit.Name;
    public Season Season => Outfit.Season;
    public OutfitScene Scene => Outfit.Scene;
    public int Rating => Outfit.Rating;
    public int WearCount => Outfit.WearCount;
    public DateTime? WornDate => Outfit.WornDate;
}
