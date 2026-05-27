using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class ImportClothesFromImagesTests
{
    [Fact]
    public async Task ExecuteAsync_SavesImagesAndAddsClothesInOneBatch()
    {
        var tagId = Guid.NewGuid();
        var repository = new FakeClothingRepository();
        var imageStorage = new FakeImageStorageService();
        var useCase = new ImportClothesFromImages(repository, imageStorage);

        var result = await useCase.ExecuteAsync(new BatchClothingImportRequest(
            [
                new BatchClothingImportItem("a.png", " 奶白短外套 "),
                new BatchClothingImportItem("b.png", "")
            ],
            ClothingType.Outerwear,
            Season.Winter,
            " 奶白 ",
            "  Uniqlo ",
            "  一批同类外套 ",
            4,
            [tagId]));

        Assert.Equal(["stored-a.png", "stored-b.png"], imageStorage.SavedImagePaths);
        Assert.Equal(2, repository.AddedClothes.Count);
        Assert.Equal(repository.AddedClothes, result.Clothes);

        var clothing = repository.AddedClothes[0];
        Assert.Equal("奶白短外套", clothing.Name);
        Assert.Equal(ClothingType.Outerwear, clothing.Type);
        Assert.Equal(Season.Winter, clothing.Season);
        Assert.Equal("stored-a.png", clothing.ImagePath);
        Assert.Equal("奶白", clothing.Color);
        Assert.Equal("Uniqlo", clothing.Brand);
        Assert.Equal("一批同类外套", clothing.Notes);
        Assert.Equal(4, clothing.FavoriteLevel);
        Assert.Equal(tagId, Assert.Single(clothing.ClothingTags).TagId);
        Assert.Equal(ImportClothesFromImages.DefaultName, repository.AddedClothes[1].Name);
    }

    [Fact]
    public async Task ExecuteAsync_DeletesSavedImagesWhenRepositoryFails()
    {
        var repository = new FakeClothingRepository { ThrowOnAddRange = true };
        var imageStorage = new FakeImageStorageService();
        var useCase = new ImportClothesFromImages(repository, imageStorage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(new BatchClothingImportRequest(
            [
                new BatchClothingImportItem("a.png", "a"),
                new BatchClothingImportItem("b.png", "b")
            ],
            ClothingType.Unspecified,
            Season.Unspecified,
            null,
            null,
            null,
            0,
            [])));

        Assert.Equal(["stored-a.png", "stored-b.png"], imageStorage.DeletedImagePaths);
        Assert.Empty(repository.AddedClothes);
    }

    private sealed class FakeClothingRepository : IClothingRepository
    {
        public IReadOnlyList<Clothing> AddedClothes => _addedClothes;
        public bool ThrowOnAddRange { get; set; }

        private readonly List<Clothing> _addedClothes = [];

        public Task<IEnumerable<Clothing>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Clothing>());
        public Task<Clothing?> GetByIdAsync(Guid id) => Task.FromResult<Clothing?>(null);
        public Task AddAsync(Clothing entity) => throw new NotImplementedException();
        public Task UpdateAsync(Clothing entity) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type) => Task.FromResult(Enumerable.Empty<Clothing>());
        public Task<IEnumerable<Clothing>> GetByTypesAsync(IEnumerable<ClothingType> types) => Task.FromResult(Enumerable.Empty<Clothing>());

        public Task AddRangeAsync(IEnumerable<Clothing> clothes)
        {
            if (ThrowOnAddRange)
                throw new InvalidOperationException("boom");

            _addedClothes.AddRange(clothes);
            return Task.CompletedTask;
        }

        public Task DeleteRangeAsync(IEnumerable<Guid> ids) => throw new NotImplementedException();
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public List<string> SavedImagePaths { get; } = [];
        public List<string> DeletedImagePaths { get; } = [];

        public Task<string> SaveImageAsync(string sourcePath)
        {
            var storedPath = $"stored-{Path.GetFileName(sourcePath)}";
            SavedImagePaths.Add(storedPath);
            return Task.FromResult(storedPath);
        }

        public Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200) => throw new NotImplementedException();
        public Task<bool> EnsureThumbnailAsync(string imagePath, int maxSize = 200) => throw new NotImplementedException();
        public Task<bool> EnsureDisplayAsync(string imagePath, int maxWidth = 900) => throw new NotImplementedException();
        public Task RestoreImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task DeleteImageAsync(string imagePath) => DeleteImageWithThumbnailAsync(imagePath);

        public Task DeleteImageWithThumbnailAsync(string imagePath)
        {
            DeletedImagePaths.Add(imagePath);
            return Task.CompletedTask;
        }

        public async Task TryDeleteImageAsync(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;
            await DeleteImageWithThumbnailAsync(imagePath);
        }

        public string GetImageFullPath(string relativePath) => relativePath;
        public string GetDisplayFullPath(string relativePath) => relativePath;
        public string GetThumbnailFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetOriginalImageFullPaths() => [];
        public IReadOnlyList<string> GetImageAssetFullPaths(string relativePath) => [];
    }
}
