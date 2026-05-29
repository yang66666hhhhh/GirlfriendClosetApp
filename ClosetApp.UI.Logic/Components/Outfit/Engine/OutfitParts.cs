using ClosetApp.Domain.Clothing;
using ClothingEntity = ClosetApp.Domain.Entities.Clothing;

namespace ClosetApp.UI.Components.Outfit.Engine;

public sealed class OutfitParts
{
    public ClothingEntity? Outer { get; init; }
    public ClothingEntity? Mid { get; init; }
    public ClothingEntity? Inner { get; init; }
    public ClothingEntity? Dress { get; init; }
    public ClothingEntity? Bottom { get; init; }
    public ClothingEntity? Shoes { get; init; }
    public ClothingEntity? Accessory { get; init; }

    public ClothingEntity? PrimaryUpper => Outer ?? Mid ?? Inner ?? Dress;
    public ClothingEntity? InnerUpper => Outer != null ? Mid ?? Inner : null;

    public static OutfitParts FromClothes(IList<ClothingEntity> clothes)
    {
        return new OutfitParts
        {
            Outer = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.OuterLayer),
            Mid = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.MidLayer),
            Inner = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.BaseTop),
            Dress = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.FullBody),
            Bottom = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.Bottom),
            Shoes = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.Footwear),
            Accessory = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.Accessory)
        };
    }

    private static LayerRole ResolveLayerRole(ClothingEntity c)
    {
        if (c.GarmentType.HasValue)
            return ClothingMappings.GetLayerRole(c.GarmentType.Value);
        return ClothingMappings.GetLayerRole(ClothingMappings.InferGarmentType(c.Type));
    }
}
