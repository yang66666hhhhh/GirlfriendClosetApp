namespace ClosetApp.UI.Components.Clothing;

public static class BatchClothingImportBuilder
{
    public const string DefaultName = "未命名";

    public static global::ClosetApp.Domain.Entities.Clothing CreateClothing(string imagePath, BatchClothingImportOptions options)
    {
        return new global::ClosetApp.Domain.Entities.Clothing
        {
            Name = DefaultName,
            Type = options.Type,
            Season = options.Season,
            ImagePath = imagePath,
            Color = Normalize(options.Color),
            Brand = Normalize(options.Brand),
            Notes = Normalize(options.Notes),
            FavoriteLevel = options.FavoriteLevel,
            IsFavorite = options.FavoriteLevel >= 4,
            ClothingTags = options.Tags
                .Select(tag => new global::ClosetApp.Domain.Entities.ClothingTag { TagId = tag.Id })
                .ToList()
        };
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
