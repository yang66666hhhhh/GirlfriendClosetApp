using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ClosetApp.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private readonly string _imageFolder;
    private readonly string _thumbnailFolder;

    public ImageStorageService()
    {
        var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(folder, "ClosetApp");
        _imageFolder = Path.Combine(appFolder, "images");
        _thumbnailFolder = Path.Combine(appFolder, "thumbnails");
        Directory.CreateDirectory(_imageFolder);
        Directory.CreateDirectory(_thumbnailFolder);
    }

    public async Task<string> SaveImageAsync(string sourcePath)
    {
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(_imageFolder, fileName);
        await using var stream = File.Create(destPath);
        await using var fileStream = File.OpenRead(sourcePath);
        await fileStream.CopyToAsync(stream);
        return fileName;
    }

    public async Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200)
    {
        var fileName = $"{Guid.NewGuid()}_thumb{Path.GetExtension(sourcePath)}";
        var destPath = Path.Combine(_thumbnailFolder, fileName);
        
        using var image = await Image.LoadAsync(sourcePath);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));
        await image.SaveAsync(destPath);
        return fileName;
    }

    public Task DeleteImageAsync(string imagePath)
    {
        var fullPath = GetImageFullPath(imagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    public Task DeleteImageWithThumbnailAsync(string imagePath)
    {
        var fullPath = GetImageFullPath(imagePath);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        var thumbPath = GetThumbnailFullPath(imagePath);
        if (File.Exists(thumbPath))
            File.Delete(thumbPath);
        return Task.CompletedTask;
    }

    public string GetImageFullPath(string relativePath)
    {
        return Path.Combine(_imageFolder, relativePath);
    }

    public string GetThumbnailFullPath(string relativePath)
    {
        var name = Path.GetFileNameWithoutExtension(relativePath);
        var ext = Path.GetExtension(relativePath);
        return Path.Combine(_thumbnailFolder, $"{name}_thumb{ext}");
    }
}