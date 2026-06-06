using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using ClosetApp.Domain.Entities;
using ClosetApp.UI.Services;

namespace ClosetApp.UI.Components.Shared.Modal;

public partial class GeneratedImagePreviewDialog : UserControl
{
    private readonly IReadOnlyList<OutfitGeneratedImage> _images;
    private Guid _selectedImageId;

    public GeneratedImagePreviewDialog(IReadOnlyList<OutfitGeneratedImage> images, Guid selectedImageId)
    {
        _images = images
            .Where(image => string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(image.ResultImagePath))
            .OrderByDescending(image => image.IsPrimary)
            .ThenByDescending(image => image.CreatedAt)
            .ToList();
        _selectedImageId = selectedImageId;
        InitializeComponent();
        Loaded += GeneratedImagePreviewDialog_Loaded;
    }

    private void GeneratedImagePreviewDialog_Loaded(object sender, RoutedEventArgs e)
    {
        HistoryList.ItemsSource = _images.Select(image => new GeneratedImageHistoryItem(
            image.Id,
            image.IsPrimary,
            image.Model,
            $"{image.CreatedAt:yyyy-MM-dd HH:mm}",
            image.IsPrimary ? "在效果图优先模式下，这张图会优先出现在搭配卡片里。" : "点击左侧缩略图可切换大图查看。",
            BuildImageBitmap(image.ResultImagePath!, 180, preferThumbnail: true)))
            .ToList();
        RenderSelectedImage();
    }

    private void HistoryItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: Guid imageId })
        {
            _selectedImageId = imageId;
            RenderSelectedImage();
        }
    }

    private void RenderSelectedImage()
    {
        var image = _images.FirstOrDefault(item => item.Id == _selectedImageId) ?? _images.FirstOrDefault();
        if (image == null)
            return;

        _selectedImageId = image.Id;
        PreviewImage.Source = BuildImageBitmap(image.ResultImagePath!, 1200, preferThumbnail: false);
        TxtSubtitle.Text = image.IsPrimary
            ? "当前查看的是这套搭配的首选效果图。"
            : "当前查看的是这套搭配的历史效果图。";
        TxtMeta.Text = $"{image.Model} · {image.CreatedAt:yyyy-MM-dd HH:mm}";
        TxtHint.Text = image.IsPrimary
            ? "在效果图优先模式下，这张图会优先显示在搭配卡片和效果图概览里。"
            : "保留历史图可以对比不同场景、构图和氛围。";
        PrimaryBadge.Visibility = image.IsPrimary ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ModalService.Instance.Hide();
    }

    private static BitmapImage? BuildImageBitmap(string relativePath, int decodePixelWidth, bool preferThumbnail)
    {
        var extension = Path.GetExtension(relativePath);
        var fileName = Path.GetFileNameWithoutExtension(relativePath);
        var thumbnailPath = Path.Combine(ClosetApp.Infrastructure.AppPaths.AiRendersThumbnailsDir, $"{fileName}_thumb{extension}");
        var displayPath = Path.Combine(ClosetApp.Infrastructure.AppPaths.AiRendersDisplayDir, relativePath);
        var imagePath = preferThumbnail
            ? File.Exists(thumbnailPath) ? thumbnailPath : displayPath
            : File.Exists(displayPath) ? displayPath : thumbnailPath;
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return null;

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imagePath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.DecodePixelWidth = decodePixelWidth;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private sealed record GeneratedImageHistoryItem(
        Guid Id,
        bool IsPrimary,
        string ModelText,
        string CreatedAtText,
        string SummaryText,
        BitmapImage? ThumbnailSource);
}
