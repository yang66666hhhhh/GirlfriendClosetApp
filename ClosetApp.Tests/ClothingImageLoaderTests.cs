using System.IO;
using System.Threading;
using ClosetApp.Application.Images;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public sealed class ClothingImageLoaderTests : IDisposable
{
    public ClothingImageLoaderTests()
    {
        ClothingImageLoader.ClearMemoryCaches();
    }

    [Fact]
    public void Load_WhenImageDecodeFails_CachesFailureForShortWindow()
    {
        var invalidImagePath = CreateInvalidImageFile();

        try
        {
            var first = ClothingImageLoader.Load(invalidImagePath, ImageVariant.Original, 320);
            var failedCached = ClothingImageLoader.HasRecentFailureForDiagnostics(
                invalidImagePath,
                ImageVariant.Original,
                320);

            var second = ClothingImageLoader.Load(invalidImagePath, ImageVariant.Original, 320);

            Assert.Null(first);
            Assert.True(failedCached);
            Assert.Null(second);
        }
        finally
        {
            File.Delete(invalidImagePath);
        }
    }

    [Fact]
    public void GetDisplaySize_WhenImageDecodeFails_DoesNotPersistNullSizeButMarksFailure()
    {
        var invalidImagePath = CreateInvalidImageFile();

        try
        {
            var size = ClothingImageLoader.GetDisplaySize(invalidImagePath, 280);

            Assert.Null(size);
            Assert.False(ClothingImageLoader.HasSizeCacheEntryForDiagnostics(invalidImagePath, 280));
            Assert.True(ClothingImageLoader.HasRecentFailureForDiagnostics(
                invalidImagePath,
                ImageVariant.Display,
                280));
        }
        finally
        {
            File.Delete(invalidImagePath);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenCanceled_ReturnsNullWithoutCachingFailure()
    {
        var invalidImagePath = CreateInvalidImageFile();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            var image = await ClothingImageLoader.LoadAsync(
                invalidImagePath,
                ImageVariant.Original,
                320,
                cancellationToken: cts.Token);

            Assert.Null(image);
            Assert.False(ClothingImageLoader.HasRecentFailureForDiagnostics(
                invalidImagePath,
                ImageVariant.Original,
                320));
        }
        finally
        {
            File.Delete(invalidImagePath);
        }
    }

    public void Dispose()
    {
        ClothingImageLoader.ClearMemoryCaches();
    }

    private static string CreateInvalidImageFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        File.WriteAllText(path, "not-a-real-image");
        return path;
    }
}
