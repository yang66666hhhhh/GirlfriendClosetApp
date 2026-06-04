using System.IO;
using System.Windows.Media.Imaging;
using ClosetApp.Domain.Entities;

namespace ClosetApp.UI.Services;

public static class OutfitGeneratedImageDisplayHelper
{
    public static IReadOnlyList<OutfitGeneratedImage> GetSucceededImages(IEnumerable<OutfitGeneratedImage>? images)
    {
        return images?
            .Where(image =>
                string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(image.ResultImagePath))
            .OrderByDescending(image => image.IsPrimary)
            .ThenByDescending(image => image.CreatedAt)
            .ToList() ?? [];
    }

    public static OutfitGeneratedImage? GetPrimaryOrFirst(IEnumerable<OutfitGeneratedImage>? images)
    {
        var list = images?.ToList() ?? [];
        return list.FirstOrDefault(image => image.IsPrimary) ?? list.FirstOrDefault();
    }

    public static BitmapImage? BuildBitmap(string relativePath, int decodePixelWidth, bool preferThumbnail)
    {
        var absolutePath = ResolveImagePath(relativePath, preferThumbnail);
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
            return null;

        return AiImageBitmapCache.GetOrLoad(absolutePath, decodePixelWidth);
    }

    public static OutfitGeneratedImageState BuildState(IEnumerable<OutfitGeneratedImage>? images)
    {
        var allImages = images?.ToList() ?? [];
        var succeededCount = allImages.Count(image =>
            string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(image.ResultImagePath));

        if (allImages.Any(image => IsPendingStatus(image.Status)))
        {
            return new OutfitGeneratedImageState("生成中", "AiState.Pending", succeededCount);
        }

        if (succeededCount > 0)
        {
            return new OutfitGeneratedImageState($"AI效果图 ×{succeededCount}", "AiState.Success", succeededCount);
        }

        if (allImages.Any(image => string.Equals(image.Status, "Failed", StringComparison.OrdinalIgnoreCase)))
        {
            return new OutfitGeneratedImageState("生成失败", "AiState.Failed", 0);
        }

        return new OutfitGeneratedImageState("未生成", "AiState.Empty", 0);
    }

    private static bool IsPendingStatus(string? status)
    {
        return string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Running", StringComparison.OrdinalIgnoreCase)
            || string.Equals(status, "Processing", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveImagePath(string relativePath, bool preferThumbnail)
    {
        var extension = Path.GetExtension(relativePath);
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        var thumbnailPath = Path.Combine(ClosetApp.Infrastructure.AppPaths.AiRendersThumbnailsDir, $"{fileName}_thumb{extension}");
        var displayPath = Path.Combine(ClosetApp.Infrastructure.AppPaths.AiRendersDisplayDir, relativePath);

        return preferThumbnail
            ? File.Exists(thumbnailPath) ? thumbnailPath : displayPath
            : File.Exists(displayPath) ? displayPath : thumbnailPath;
    }
}

public sealed record OutfitGeneratedImageState(string Label, string VisualStateKey, int SucceededCount);
