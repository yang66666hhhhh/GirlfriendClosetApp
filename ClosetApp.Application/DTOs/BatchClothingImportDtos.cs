using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record BatchClothingImportRequest(
    IReadOnlyList<BatchClothingImportItem> Items,
    ClothingType Type,
    Season Season,
    string? Color,
    string? Brand,
    string? Notes,
    int FavoriteLevel,
    IReadOnlyList<Guid> TagIds);

public sealed record BatchClothingImportResult(
    IReadOnlyList<global::ClosetApp.Domain.Entities.Clothing> Clothes);

public sealed record BatchClothingImportItem(
    string SourceImagePath,
    string? Name);
