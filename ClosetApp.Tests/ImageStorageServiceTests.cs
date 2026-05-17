using System.IO;
using ClosetApp.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ClosetApp.Tests;

public class ImageStorageServiceTests
{
    [Fact]
    public async Task SaveImageAsync_CreatesThumbnailWithExpectedMaxSize()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new ImageStorageService(tempDir);
            var sourcePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourcePath, width: 640, height: 320);

            var storedFileName = await service.SaveImageAsync(sourcePath);
            var imagePath = service.GetImageFullPath(storedFileName);
            var thumbnailPath = service.GetThumbnailFullPath(storedFileName);

            Assert.True(File.Exists(imagePath));
            Assert.True(File.Exists(thumbnailPath));

            using var thumbnail = await Image.LoadAsync<Rgba32>(thumbnailPath);
            Assert.True(thumbnail.Width <= 200);
            Assert.True(thumbnail.Height <= 200);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteImageWithThumbnailAsync_RemovesStoredFiles()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new ImageStorageService(tempDir);
            var sourcePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourcePath, width: 320, height: 320);

            var storedFileName = await service.SaveImageAsync(sourcePath);

            await service.DeleteImageWithThumbnailAsync(storedFileName);

            Assert.False(File.Exists(service.GetImageFullPath(storedFileName)));
            Assert.False(File.Exists(service.GetThumbnailFullPath(storedFileName)));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task DeleteImageAsync_AlsoRemovesThumbnail()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new ImageStorageService(tempDir);
            var sourcePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourcePath, width: 320, height: 320);

            var storedFileName = await service.SaveImageAsync(sourcePath);

            await service.DeleteImageAsync(storedFileName);

            Assert.False(File.Exists(service.GetImageFullPath(storedFileName)));
            Assert.False(File.Exists(service.GetThumbnailFullPath(storedFileName)));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveThumbnailAsync_UsesRequestedMaxSize()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new ImageStorageService(tempDir);
            var sourcePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourcePath, width: 720, height: 360);

            var thumbnailFileName = await service.SaveThumbnailAsync(sourcePath, maxSize: 120);
            var thumbnailPath = Path.Combine(tempDir, "thumbnails", thumbnailFileName);

            Assert.True(File.Exists(thumbnailPath));

            using var thumbnail = await Image.LoadAsync<Rgba32>(thumbnailPath);
            Assert.True(thumbnail.Width <= 120);
            Assert.True(thumbnail.Height <= 120);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureThumbnailAsync_RebuildsMissingThumbnailFromStoredImage()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new ImageStorageService(tempDir);
            var sourcePath = Path.Combine(tempDir, "source.png");
            await CreateSourceImageAsync(sourcePath, width: 420, height: 420);

            var storedFileName = await service.SaveImageAsync(sourcePath);
            var thumbnailPath = service.GetThumbnailFullPath(storedFileName);
            File.Delete(thumbnailPath);

            var rebuilt = await service.EnsureThumbnailAsync(storedFileName, maxSize: 140);

            Assert.True(rebuilt);
            Assert.True(File.Exists(thumbnailPath));

            using var thumbnail = await Image.LoadAsync<Rgba32>(thumbnailPath);
            Assert.True(thumbnail.Width <= 140);
            Assert.True(thumbnail.Height <= 140);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task EnsureThumbnailAsync_ReturnsFalseWhenStoredImageMissing()
    {
        var tempDir = CreateTempDir();

        try
        {
            var service = new ImageStorageService(tempDir);

            var rebuilt = await service.EnsureThumbnailAsync("missing.png");

            Assert.False(rebuilt);
            Assert.False(File.Exists(service.GetThumbnailFullPath("missing.png")));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    private static async Task CreateSourceImageAsync(string path, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        image.ProcessPixelRows(accessor =>
        {
            for (var y = 60; y < height - 60; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 40; x < width - 40; x++)
                {
                    row[x] = new Rgba32(230, 120, 140, 255);
                }
            }
        });

        await image.SaveAsPngAsync(path);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "ClosetApp.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
