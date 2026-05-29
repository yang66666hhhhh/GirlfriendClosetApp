using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Clothing;

public sealed record BatchClothingImportOptions(
    ClothingType Type,
    Season Season,
    string? Color,
    string? Brand,
    string? Notes,
    int FavoriteLevel,
    IReadOnlyList<Tag> Tags);
