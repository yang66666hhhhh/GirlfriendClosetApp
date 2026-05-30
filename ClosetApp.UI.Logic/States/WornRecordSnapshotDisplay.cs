using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Logic.States;

public sealed record WornRecordSnapshotDisplay(
    string OutfitName,
    IList<Clothing> PreviewClothes,
    int SnapshotCount,
    int CurrentCount,
    bool IsDeleted,
    bool IsChanged,
    bool HasUsableSnapshot)
{
    public bool ShouldShowSnapshotStatus => IsDeleted || IsChanged;
}

public static class WornRecordSnapshotDisplayFactory
{
    public static WornRecordSnapshotDisplay FromRecord(OutfitWornRecord record)
    {
        var liveClothes = GetLiveClothes(record);
        var snapshotClothes = TryGetSnapshotClothes(record);
        var hasUsableSnapshot = snapshotClothes.Count > 0;
        var isDeleted = record.Outfit == null;
        var currentCount = liveClothes.Count;
        var snapshotCount = ResolveSnapshotCount(record, snapshotClothes);
        var isChanged = !isDeleted && snapshotCount > 0 && currentCount != snapshotCount;
        var displayName = !string.IsNullOrWhiteSpace(record.OutfitNameSnapshot)
            ? record.OutfitNameSnapshot
            : ResolveLiveOutfitName(record.Outfit);

        return new WornRecordSnapshotDisplay(
            displayName,
            hasUsableSnapshot ? snapshotClothes : liveClothes,
            snapshotCount,
            currentCount,
            isDeleted,
            isChanged,
            hasUsableSnapshot);
    }

    private static IList<Clothing> GetLiveClothes(OutfitWornRecord record)
    {
        return record.Outfit?.OutfitClothes
            .Select(link => link.Clothing)
            .Where(clothing => clothing != null)
            .Cast<Clothing>()
            .ToList() ?? [];
    }

    private static IList<Clothing> TryGetSnapshotClothes(OutfitWornRecord record)
    {
        if (!record.IsSnapshotComplete || string.IsNullOrWhiteSpace(record.ClothingDetailsSnapshot))
            return [];

        try
        {
            var snapshotClothes = JsonSerializer.Deserialize<List<ClothingSnapshotDto>>(record.ClothingDetailsSnapshot);
            return snapshotClothes?
                .Where(dto => dto.Id != Guid.Empty || !string.IsNullOrWhiteSpace(dto.Name))
                .Select(ToClothing)
                .ToList() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static Clothing ToClothing(ClothingSnapshotDto dto)
    {
        var type = ParseEnum(dto.Type, ClothingType.Unspecified);
        var garmentType = ParseNullableEnum<GarmentType>(dto.GarmentType)
            ?? InferGarmentType(type, dto.Name);

        return new Clothing
        {
            Id = dto.Id,
            Name = dto.Name,
            ImagePath = dto.ImagePath,
            Color = dto.Color,
            Type = type == ClothingType.Unspecified && garmentType.HasValue
                ? InferClothingType(garmentType.Value)
                : type,
            GarmentType = garmentType
        };
    }

    private static GarmentType? InferGarmentType(ClothingType type, string? name)
    {
        if (type != ClothingType.Unspecified)
            return ClothingMappings.InferGarmentType(type);

        var normalizedName = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
            return null;

        if (normalizedName.Contains("半裙") || normalizedName.Contains("短裙") || normalizedName.Contains("长裙") || normalizedName.Contains("裙"))
            return GarmentType.Skirt;
        if (normalizedName.Contains("裤"))
            return GarmentType.Trousers;
        if (normalizedName.Contains("鞋") || normalizedName.Contains("靴"))
            return GarmentType.Sneakers;
        if (normalizedName.Contains("包"))
            return GarmentType.Bag;
        if (normalizedName.Contains("连衣裙") || normalizedName.Contains("裙装"))
            return GarmentType.Dress;
        if (normalizedName.Contains("外套") || normalizedName.Contains("大衣") || normalizedName.Contains("西装"))
            return GarmentType.Jacket;

        return null;
    }

    private static ClothingType InferClothingType(GarmentType garmentType)
    {
        return ClothingMappings.GetDisplayCategory(garmentType) switch
        {
            DisplayCategory.Bottom when garmentType == GarmentType.Skirt => ClothingType.Skirt,
            DisplayCategory.Bottom => ClothingType.Bottom,
            DisplayCategory.Dress => ClothingType.Dress,
            DisplayCategory.Footwear => ClothingType.Shoes,
            DisplayCategory.Accessory => ClothingType.Accessory,
            _ when ClothingMappings.GetLayerRole(garmentType) == LayerRole.OuterLayer => ClothingType.Outerwear,
            _ => ClothingType.Top
        };
    }

    private static int ResolveSnapshotCount(OutfitWornRecord record, IList<Clothing> snapshotClothes)
    {
        if (record.ClothingCountSnapshot > 0)
            return record.ClothingCountSnapshot;

        return snapshotClothes.Count;
    }

    private static string ResolveLiveOutfitName(Outfit? outfit)
    {
        var name = outfit?.Name?.Trim();
        return string.IsNullOrWhiteSpace(name) ? "未命名搭配" : name;
    }

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : fallback;
    }

    private static TEnum? ParseNullableEnum<TEnum>(string? value)
        where TEnum : struct, Enum
    {
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed)
            ? parsed
            : null;
    }
}
