using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.DTOs;

public sealed record BatchWardrobeClearRequest(
    IReadOnlyList<ClothingType> Types);

public sealed record BatchWardrobeClearResult(
    int DeletedCount);
