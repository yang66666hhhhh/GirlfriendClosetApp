using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Components.Outfit.Editor;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitSelectionRulesTests
{
    [Fact]
    public void SelectingDress_DoesNotClearOuterwear()
    {
        var dress = Clothing("Dress", ClothingType.Dress, GarmentType.Dress);
        var outerwear = Clothing("Coat", ClothingType.Outerwear, GarmentType.Coat);

        Assert.False(OutfitSelectionRules.ShouldClearWhenSelecting(dress, outerwear));
        Assert.False(OutfitSelectionRules.ShouldClearWhenSelecting(outerwear, dress));
    }

    [Fact]
    public void SelectingDress_ClearsTopAndLowerBody()
    {
        var dress = Clothing("Dress", ClothingType.Dress, GarmentType.Dress);
        var top = Clothing("Shirt", ClothingType.Top, GarmentType.Shirt);
        var pants = Clothing("Pants", ClothingType.Bottom, GarmentType.Trousers);
        var skirt = Clothing("Skirt", ClothingType.Skirt, GarmentType.Skirt);

        Assert.True(OutfitSelectionRules.ShouldClearWhenSelecting(dress, top));
        Assert.True(OutfitSelectionRules.ShouldClearWhenSelecting(dress, pants));
        Assert.True(OutfitSelectionRules.ShouldClearWhenSelecting(dress, skirt));
    }

    [Fact]
    public void SelectingPantsAndSkirt_UsesSameLowerBodySlot()
    {
        var pants = Clothing("Pants", ClothingType.Bottom, GarmentType.Trousers);
        var skirt = Clothing("Skirt", ClothingType.Skirt, GarmentType.Skirt);

        Assert.Equal(OutfitSelectionSlot.LowerBody, OutfitSelectionRules.GetSlot(pants));
        Assert.Equal(OutfitSelectionSlot.LowerBody, OutfitSelectionRules.GetSlot(skirt));
        Assert.True(OutfitSelectionRules.ShouldClearWhenSelecting(pants, skirt));
        Assert.True(OutfitSelectionRules.ShouldClearWhenSelecting(skirt, pants));
    }

    [Fact]
    public void SelectingAccessory_DoesNotClearAnotherAccessory()
    {
        var bag = Clothing("Bag", ClothingType.Accessory, GarmentType.Bag);
        var hat = Clothing("Hat", ClothingType.Accessory, GarmentType.Hat);

        Assert.False(OutfitSelectionRules.ShouldClearWhenSelecting(bag, hat));
    }

    private static Clothing Clothing(string name, ClothingType type, GarmentType garmentType)
    {
        return new Clothing
        {
            Name = name,
            Type = type,
            GarmentType = garmentType,
            Season = Season.AllSeason
        };
    }
}
