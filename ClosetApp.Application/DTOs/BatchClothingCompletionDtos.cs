using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record BatchClothingCompletionRequest(
    IReadOnlyList<Guid> ClothingIds,
    ClothingType? Type,
    Season? Season,
    string? Color,
    string? Brand,
    IReadOnlyList<Guid> TagIds);

public sealed record BatchClothingCompletionResult(
    int UpdatedCount,
    int SkippedCount);
