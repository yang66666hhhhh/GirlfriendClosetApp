using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Components.Outfit.Engine;
using Xunit;
using ClothingEntity = ClosetApp.Domain.Entities.Clothing;

namespace ClosetApp.Tests;

public class OutfitCompositionEngineTests
{
    private const double CanvasWidth = 280;
    private const double CanvasHeight = 360;

    private readonly OutfitCompositionEngine _engine = new();

    [Fact]
    public void DetermineMode_WithEmptyClothes_ReturnsSolo()
    {
        var mode = _engine.DetermineMode([]);

        Assert.Equal(CompositionMode.Solo, mode);
    }

    [Fact]
    public void DetermineMode_WithOnlyAccessory_ReturnsSolo()
    {
        var mode = _engine.DetermineMode([Clothing("Bag", GarmentType.Bag)]);

        Assert.Equal(CompositionMode.Solo, mode);
    }

    [Fact]
    public void DetermineMode_WithDress_ReturnsDress()
    {
        var mode = _engine.DetermineMode([
            Clothing("Dress", GarmentType.Dress),
            Clothing("Heels", GarmentType.Heels)
        ]);

        Assert.Equal(CompositionMode.Dress, mode);
    }

    [Fact]
    public void DetermineMode_WithTopAndBottom_ReturnsTopBottom()
    {
        var mode = _engine.DetermineMode([
            Clothing("Shirt", GarmentType.Shirt),
            Clothing("Jeans", GarmentType.Jeans)
        ]);

        Assert.Equal(CompositionMode.TopBottom, mode);
    }

    [Fact]
    public void DetermineMode_WithTopOnly_ReturnsMixed()
    {
        var mode = _engine.DetermineMode([
            Clothing("Shirt", GarmentType.Shirt),
            Clothing("Sneakers", GarmentType.Sneakers)
        ]);

        Assert.Equal(CompositionMode.Mixed, mode);
    }

    [Fact]
    public void DetermineMode_UsesLegacyTypeFallback()
    {
        var mode = _engine.DetermineMode([
            LegacyClothing("Top", ClothingType.Top),
            LegacyClothing("Skirt", ClothingType.Skirt)
        ]);

        Assert.Equal(CompositionMode.TopBottom, mode);
    }

    [Fact]
    public void CalculateLayout_WithEmptyClothes_ReturnsEmptyLayout()
    {
        var layout = _engine.CalculateLayout([], CanvasWidth, CanvasHeight);

        Assert.Empty(layout);
    }

    [Fact]
    public void CalculateLayout_WithSoloClothing_ReturnsOneCenteredItem()
    {
        var top = Clothing("Top", GarmentType.TShirt);

        var layout = _engine.CalculateLayout([top], CanvasWidth, CanvasHeight);

        var item = Assert.Single(layout);
        Assert.Same(top, item.Clothing);
        Assert.True(item.Width > 0);
        Assert.True(item.Height > 0);
        Assert.InRange(item.Y, 0, CanvasHeight);
    }

    [Fact]
    public void CalculateLayout_WithDressOutfit_OrdersDressShoesAndAccessory()
    {
        var dress = Clothing("Dress", GarmentType.Dress);
        var shoes = Clothing("Shoes", GarmentType.Heels);
        var accessory = Clothing("Bag", GarmentType.Bag);

        var layout = _engine.CalculateLayout([dress, shoes, accessory], CanvasWidth, CanvasHeight);

        Assert.Equal(3, layout.Count);
        Assert.Contains(layout, item => item.Clothing == dress && item.ZIndex == 2);
        Assert.Contains(layout, item => item.Clothing == shoes && item.ZIndex == 3);
        Assert.Contains(layout, item => item.Clothing == accessory && item.ZIndex == 4);
        Assert.True(layout.Single(item => item.Clothing == shoes).Y >= layout.Single(item => item.Clothing == dress).Y);
    }

    [Fact]
    public void CalculateLayout_WithTopBottomOutfit_StacksTopBottomAndShoes()
    {
        var top = Clothing("Top", GarmentType.Blouse);
        var bottom = Clothing("Bottom", GarmentType.Trousers);
        var shoes = Clothing("Shoes", GarmentType.Loafers);

        var layout = _engine.CalculateLayout([top, bottom, shoes], CanvasWidth, CanvasHeight);

        Assert.Equal(3, layout.Count);
        var topItem = layout.Single(item => item.Clothing == top);
        var bottomItem = layout.Single(item => item.Clothing == bottom);
        var shoesItem = layout.Single(item => item.Clothing == shoes);
        Assert.True(topItem.Y < bottomItem.Y);
        Assert.True(bottomItem.Y < shoesItem.Y);
    }

    [Fact]
    public void CalculateLayout_WithMixedOutfit_UsesMixedLayoutInsteadOfSoloOnly()
    {
        var top = Clothing("Top", GarmentType.Shirt);
        var shoes = Clothing("Shoes", GarmentType.Sneakers);
        var accessory = Clothing("Hat", GarmentType.Hat);

        var layout = _engine.CalculateLayout([top, shoes, accessory], CanvasWidth, CanvasHeight);

        Assert.Equal(3, layout.Count);
        Assert.Contains(layout, item => item.Clothing == top);
        Assert.Contains(layout, item => item.Clothing == shoes);
        Assert.Contains(layout, item => item.Clothing == accessory);
    }

    [Fact]
    public void CalculateLayout_WithExtraItems_UsesOneItemPerSupportedRole()
    {
        var clothes = new[]
        {
            Clothing("First Top", GarmentType.Shirt),
            Clothing("Second Top", GarmentType.Blouse),
            Clothing("Bottom", GarmentType.Jeans),
            Clothing("First Shoes", GarmentType.Sneakers),
            Clothing("Second Shoes", GarmentType.Boots),
            Clothing("First Accessory", GarmentType.Bag),
            Clothing("Second Accessory", GarmentType.Hat)
        };

        var layout = _engine.CalculateLayout(clothes, CanvasWidth, CanvasHeight);

        Assert.Equal(4, layout.Count);
        Assert.Contains(layout, item => item.Clothing == clothes[0]);
        Assert.Contains(layout, item => item.Clothing == clothes[2]);
        Assert.Contains(layout, item => item.Clothing == clothes[3]);
        Assert.Contains(layout, item => item.Clothing == clothes[5]);
    }

    private static ClothingEntity Clothing(string name, GarmentType garmentType)
    {
        return new ClothingEntity
        {
            Name = name,
            Type = ClothingMappings.InferGarmentType(ClothingType.Top) == garmentType
                ? ClothingType.Top
                : ClothingType.Accessory,
            GarmentType = garmentType,
            Season = Season.AllSeason
        };
    }

    private static ClothingEntity LegacyClothing(string name, ClothingType clothingType)
    {
        return new ClothingEntity
        {
            Name = name,
            Type = clothingType,
            Season = Season.AllSeason
        };
    }
}
