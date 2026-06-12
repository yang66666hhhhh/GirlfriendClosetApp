using System.IO;
using ClosetApp.UI.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ClosetApp.Tests;

public class PreviewImageSourceFactoryTests
{
    [Fact]
    public async Task TryCreateNormalizedPngBytes_UsesImageContentEvenWhenFileHasNoImageExtension()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"closet-preview-loader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourcePath = Path.Combine(tempDir, "avatar.thumb_400_0");

        try
        {
            using (var image = new Image<Rgba32>(36, 48, new Rgba32(110, 150, 210)))
            {
                await image.SaveAsJpegAsync(sourcePath, new JpegEncoder { Quality = 90 });
            }

            var bytes = PreviewImageSourceFactory.TryCreateNormalizedPngBytes(sourcePath);

            Assert.NotNull(bytes);
            Assert.True(bytes!.Length > 0);
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal(0x50, bytes[1]);
            Assert.Equal(0x4E, bytes[2]);
            Assert.Equal(0x47, bytes[3]);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public void TryCreateNormalizedPngBytes_InvalidFile_ReturnsNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"closet-preview-loader-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourcePath = Path.Combine(tempDir, "not-image.thumb_400_0");
        File.WriteAllText(sourcePath, "not-an-image");

        try
        {
            var bytes = PreviewImageSourceFactory.TryCreateNormalizedPngBytes(sourcePath);

            Assert.Null(bytes);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task TryCreateBitmapSource_ReusesCachedBitmapForSameFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"closet-preview-cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourcePath = Path.Combine(tempDir, "avatar.jpg");

        try
        {
            using (var image = new Image<Rgba32>(72, 72, new Rgba32(180, 120, 210)))
            {
                await image.SaveAsJpegAsync(sourcePath, new JpegEncoder { Quality = 90 });
            }

            var first = PreviewImageSourceFactory.TryCreateBitmapSource(sourcePath, decodePixelWidth: 160);
            var second = PreviewImageSourceFactory.TryCreateBitmapSource(sourcePath, decodePixelWidth: 160);

            Assert.NotNull(first);
            Assert.Same(first, second);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }
}
