using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Clothing;

public static class ClothingMappings
{
    public static DisplayCategory GetDisplayCategory(GarmentType type) => type switch
    {
        // Tops → Topwear
        GarmentType.TShirt or GarmentType.Shirt or GarmentType.Blouse or
        GarmentType.Knitwear or GarmentType.Hoodie or GarmentType.Sweater or
        GarmentType.TankTop => DisplayCategory.Topwear,

        // Outerwear → Topwear
        GarmentType.Jacket or GarmentType.Coat or GarmentType.Blazer or
        GarmentType.Cardigan or GarmentType.Vest => DisplayCategory.Topwear,

        // Bottoms → Bottom
        GarmentType.Jeans or GarmentType.Trousers or
        GarmentType.Shorts or GarmentType.Skirt => DisplayCategory.Bottom,

        // Full-body
        GarmentType.Dress or GarmentType.Jumpsuit => DisplayCategory.Dress,

        // Footwear
        GarmentType.Sneakers or GarmentType.Boots or GarmentType.Heels or
        GarmentType.Sandals or GarmentType.Loafers => DisplayCategory.Footwear,

        // Accessories
        GarmentType.Bag or GarmentType.Necklace or GarmentType.Hat or
        GarmentType.Belt or GarmentType.Scarf or GarmentType.Earrings or
        GarmentType.Watch or GarmentType.Sunglasses => DisplayCategory.Accessory,

        _ => DisplayCategory.Topwear
    };

    public static LayerRole GetLayerRole(GarmentType type) => type switch
    {
        GarmentType.TShirt or GarmentType.Shirt or GarmentType.Blouse or
        GarmentType.TankTop => LayerRole.BaseTop,

        GarmentType.Knitwear or GarmentType.Hoodie or
        GarmentType.Sweater => LayerRole.MidLayer,

        GarmentType.Jacket or GarmentType.Coat or GarmentType.Blazer or
        GarmentType.Cardigan or GarmentType.Vest => LayerRole.OuterLayer,

        GarmentType.Jeans or GarmentType.Trousers or
        GarmentType.Shorts or GarmentType.Skirt => LayerRole.Bottom,

        GarmentType.Dress or GarmentType.Jumpsuit => LayerRole.FullBody,

        GarmentType.Sneakers or GarmentType.Boots or GarmentType.Heels or
        GarmentType.Sandals or GarmentType.Loafers => LayerRole.Footwear,

        GarmentType.Bag or GarmentType.Necklace or GarmentType.Hat or
        GarmentType.Belt or GarmentType.Scarf or GarmentType.Earrings or
        GarmentType.Watch or GarmentType.Sunglasses => LayerRole.Accessory,

        _ => LayerRole.BaseTop
    };

    public static GarmentType InferGarmentType(ClothingType legacy) => legacy switch
    {
        ClothingType.Top => GarmentType.TShirt,
        ClothingType.Outerwear => GarmentType.Jacket,
        ClothingType.Bottom => GarmentType.Trousers,
        ClothingType.Skirt => GarmentType.Skirt,
        ClothingType.Dress => GarmentType.Dress,
        ClothingType.Shoes => GarmentType.Sneakers,
        ClothingType.Accessory => GarmentType.Bag,
        _ => GarmentType.TShirt
    };

    public static DisplayCategory? TryGetDisplayCategory(global::ClosetApp.Domain.Entities.Clothing clothing)
    {
        if (clothing.GarmentType.HasValue)
            return GetDisplayCategory(clothing.GarmentType.Value);

        if (clothing.Type == ClothingType.Unspecified)
            return null;

        return GetDisplayCategory(InferGarmentType(clothing.Type));
    }

    public static string GetDisplayName(GarmentType type) => type switch
    {
        GarmentType.TShirt => "T恤",
        GarmentType.Shirt => "衬衫",
        GarmentType.Blouse => "衬衫",
        GarmentType.Knitwear => "针织",
        GarmentType.Hoodie => "卫衣",
        GarmentType.Sweater => "毛衣",
        GarmentType.TankTop => "背心",
        GarmentType.Jacket => "外套",
        GarmentType.Coat => "大衣",
        GarmentType.Blazer => "西装",
        GarmentType.Cardigan => "开衫",
        GarmentType.Vest => "马甲",
        GarmentType.Jeans => "牛仔裤",
        GarmentType.Trousers => "裤子",
        GarmentType.Shorts => "短裤",
        GarmentType.Skirt => "短裙",
        GarmentType.Dress => "连衣裙",
        GarmentType.Jumpsuit => "连体裤",
        GarmentType.Sneakers => "运动鞋",
        GarmentType.Boots => "靴子",
        GarmentType.Heels => "高跟鞋",
        GarmentType.Sandals => "凉鞋",
        GarmentType.Loafers => "乐福鞋",
        GarmentType.Bag => "包包",
        GarmentType.Necklace => "项链",
        GarmentType.Hat => "帽子",
        GarmentType.Belt => "腰带",
        GarmentType.Scarf => "围巾",
        GarmentType.Earrings => "耳饰",
        GarmentType.Watch => "手表",
        GarmentType.Sunglasses => "墨镜",
        _ => type.ToString()
    };
}
