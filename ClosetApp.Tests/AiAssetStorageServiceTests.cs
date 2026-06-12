using System.IO;
using ClosetApp.Application.Interfaces;
using ClosetApp.Infrastructure.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace ClosetApp.Tests;

public class AiAssetStorageServiceTests
{
    [Fact]
    public async Task SaveProfileReferenceImageAsync_NormalizesToPngForReliablePreview()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"closet-ai-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourcePath = Path.Combine(tempDir, "avatar.jpg");

        try
        {
            using (var image = new Image<Rgba32>(32, 32, new Rgba32(120, 160, 210)))
            {
                await image.SaveAsJpegAsync(sourcePath, new JpegEncoder { Quality = 90 });
            }

            var service = new AiAssetStorageService();
            var slotName = $"test-user-{Guid.NewGuid():N}";

            var storedFileName = await service.SaveProfileReferenceImageAsync(sourcePath, slotName);

            Assert.Equal(".png", Path.GetExtension(storedFileName));

            var storedPath = service.GetProfileReferenceFullPath(storedFileName);
            Assert.True(File.Exists(storedPath));

            using var storedImage = await Image.LoadAsync<Rgba32>(storedPath);
            Assert.Equal(32, storedImage.Width);
            Assert.Equal(32, storedImage.Height);

            await service.TryDeleteProfileReferenceImageAsync(storedFileName);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveGeneratedImageAsync_WithCurrentUserContext_StoresAssetsUnderUserDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"closet-ai-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourcePath = Path.Combine(tempDir, "render.jpg");
        var userId = Guid.NewGuid();

        try
        {
            using (var image = new Image<Rgba32>(48, 64, new Rgba32(210, 160, 120)))
            {
                await image.SaveAsJpegAsync(sourcePath, new JpegEncoder { Quality = 90 });
            }

            var service = new AiAssetStorageService(tempDir, new StaticCurrentUserContext(userId));
            var storedFileName = await service.SaveGeneratedImageAsync(await File.ReadAllBytesAsync(sourcePath), "image/jpeg");

            var expectedUserRoot = Path.Combine(tempDir, "users", userId.ToString("N"));
            Assert.StartsWith(expectedUserRoot, service.GetGeneratedImageFullPath(storedFileName));
            Assert.All(service.GetGeneratedImageAssetFullPaths(storedFileName), path => Assert.StartsWith(expectedUserRoot, path));
            Assert.True(File.Exists(service.GetGeneratedImageFullPath(storedFileName)));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task SaveProfileReferenceImageAsync_WithExplicitUserId_StoresAssetUnderSpecifiedUserDirectory()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"closet-ai-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var sourcePath = Path.Combine(tempDir, "avatar.jpg");
        var currentUserId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();

        try
        {
            using (var image = new Image<Rgba32>(40, 40, new Rgba32(180, 120, 210)))
            {
                await image.SaveAsJpegAsync(sourcePath, new JpegEncoder { Quality = 90 });
            }

            var service = new AiAssetStorageService(tempDir, new StaticCurrentUserContext(currentUserId));
            var storedFileName = await service.SaveProfileReferenceImageAsync(sourcePath, "member-avatar", targetUserId);

            var resolvedPath = service.GetProfileReferenceFullPath(storedFileName, targetUserId);
            var unexpectedPath = service.GetProfileReferenceFullPath(storedFileName, currentUserId);

            Assert.StartsWith(Path.Combine(tempDir, "users", targetUserId.ToString("N")), resolvedPath);
            Assert.True(File.Exists(resolvedPath));
            Assert.False(File.Exists(unexpectedPath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private sealed class StaticCurrentUserContext : ICurrentUserContext
    {
        private readonly Guid _userId;

        public StaticCurrentUserContext(Guid userId)
        {
            _userId = userId;
        }

        public event EventHandler<CurrentUserChangedEventArgs>? CurrentUserChanged;

        public Task<Guid?> GetCurrentUserIdAsync() => Task.FromResult<Guid?>(_userId);

        public Task<Guid> GetRequiredCurrentUserIdAsync() => Task.FromResult(_userId);

        public Task<Guid> GetRequiredStoredUserIdAsync() => Task.FromResult(_userId);

        public Task SetCurrentUserIdAsync(Guid userId)
        {
            CurrentUserChanged?.Invoke(this, new CurrentUserChangedEventArgs(userId));
            return Task.CompletedTask;
        }
    }
}
