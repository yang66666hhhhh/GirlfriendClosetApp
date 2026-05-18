namespace ClosetApp.Domain.Clothing;

public static class ClothingTaxonomy
{
    public static readonly Dictionary<DisplayCategory, GarmentType[]> CategoryGarments = new()
    {
        [DisplayCategory.Topwear] = new[]
        {
            GarmentType.TShirt, GarmentType.Shirt, GarmentType.Blouse,
            GarmentType.Knitwear, GarmentType.Hoodie, GarmentType.Sweater, GarmentType.TankTop,
            GarmentType.Jacket, GarmentType.Coat, GarmentType.Blazer, GarmentType.Cardigan, GarmentType.Vest
        },
        [DisplayCategory.Bottom] = new[]
        {
            GarmentType.Jeans, GarmentType.Trousers, GarmentType.Shorts, GarmentType.Skirt
        },
        [DisplayCategory.Dress] = new[]
        {
            GarmentType.Dress, GarmentType.Jumpsuit
        },
        [DisplayCategory.Footwear] = new[]
        {
            GarmentType.Sneakers, GarmentType.Boots, GarmentType.Heels,
            GarmentType.Sandals, GarmentType.Loafers
        },
        [DisplayCategory.Accessory] = new[]
        {
            GarmentType.Bag, GarmentType.Necklace, GarmentType.Hat, GarmentType.Belt,
            GarmentType.Scarf, GarmentType.Earrings, GarmentType.Watch, GarmentType.Sunglasses
        }
    };

    public static IEnumerable<GarmentType> GetGarmentTypes(DisplayCategory category)
        => CategoryGarments.TryGetValue(category, out var types) ? types : Array.Empty<GarmentType>();

    public static DisplayCategory GetDisplayCategory(GarmentType type)
        => ClothingMappings.GetDisplayCategory(type);

    public static LayerRole GetLayerRole(GarmentType type)
        => ClothingMappings.GetLayerRole(type);

    public static string GetDisplayName(GarmentType type)
        => ClothingMappings.GetDisplayName(type);

    public static IEnumerable<GarmentType> FilterByCategory(
        IEnumerable<GarmentType> garments,
        DisplayCategory category)
        => garments.Where(g => ClothingMappings.GetDisplayCategory(g) == category);

    public static DisplayCategory EmojiForCategory(DisplayCategory category) => category switch
    {
        DisplayCategory.Topwear => category,
        DisplayCategory.Bottom => category,
        DisplayCategory.Dress => category,
        DisplayCategory.Footwear => category,
        DisplayCategory.Accessory => category,
        _ => category
    };

    public static string LabelForCategory(DisplayCategory category) => category switch
    {
        DisplayCategory.Topwear => "上衣",
        DisplayCategory.Bottom => "裤装",
        DisplayCategory.Dress => "连衣裙",
        DisplayCategory.Footwear => "鞋子",
        DisplayCategory.Accessory => "配饰",
        _ => category.ToString()
    };
}
