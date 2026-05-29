using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Enums;
using ClothingEntity = ClosetApp.Domain.Entities.Clothing;

namespace ClosetApp.UI.Logic.Components.Outfit.Editor;

public enum OutfitSelectionSlot
{
    Unknown,
    Top,
    Outerwear,
    LowerBody,
    Dress,
    Footwear,
    Accessory
}

public static class OutfitSelectionRules
{
    public static OutfitSelectionSlot GetSlot(ClothingEntity clothing)
    {
        if (clothing.Type == ClothingType.Unspecified && !clothing.GarmentType.HasValue)
            return OutfitSelectionSlot.Unknown;

        var role = ResolveLayerRole(clothing);
        return role switch
        {
            LayerRole.BaseTop or LayerRole.MidLayer => OutfitSelectionSlot.Top,
            LayerRole.OuterLayer => OutfitSelectionSlot.Outerwear,
            LayerRole.Bottom => OutfitSelectionSlot.LowerBody,
            LayerRole.FullBody => OutfitSelectionSlot.Dress,
            LayerRole.Footwear => OutfitSelectionSlot.Footwear,
            LayerRole.Accessory => OutfitSelectionSlot.Accessory,
            _ => OutfitSelectionSlot.Unknown
        };
    }

    public static bool ShouldClearWhenSelecting(ClothingEntity selected, ClothingEntity candidate)
    {
        var selectedSlot = GetSlot(selected);
        var candidateSlot = GetSlot(candidate);

        if (selectedSlot == OutfitSelectionSlot.Dress)
            return candidateSlot is OutfitSelectionSlot.Top or OutfitSelectionSlot.LowerBody or OutfitSelectionSlot.Dress;

        if (selectedSlot is OutfitSelectionSlot.Top or OutfitSelectionSlot.LowerBody)
            return candidateSlot == OutfitSelectionSlot.Dress || candidateSlot == selectedSlot;

        if (selectedSlot is OutfitSelectionSlot.Outerwear or OutfitSelectionSlot.Footwear)
            return candidateSlot == selectedSlot;

        return false;
    }

    public static bool DisablesTopOrLowerBody(IReadOnlyCollection<ClothingEntity> selected)
    {
        return selected.Any(clothing => GetSlot(clothing) == OutfitSelectionSlot.Dress);
    }

    public static bool DisablesDress(IReadOnlyCollection<ClothingEntity> selected)
    {
        return selected.Any(clothing => GetSlot(clothing) is OutfitSelectionSlot.Top or OutfitSelectionSlot.LowerBody);
    }

    private static LayerRole ResolveLayerRole(ClothingEntity clothing)
    {
        if (clothing.GarmentType.HasValue)
            return ClothingMappings.GetLayerRole(clothing.GarmentType.Value);

        return ClothingMappings.GetLayerRole(ClothingMappings.InferGarmentType(clothing.Type));
    }
}
