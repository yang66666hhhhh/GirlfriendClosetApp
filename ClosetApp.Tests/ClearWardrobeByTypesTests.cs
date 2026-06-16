using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class ClearWardrobeByTypesTests
{
    [Fact]
    public async Task ExecuteAsync_DeletesSelectedTypes_AndCleansImagesOnce()
    {
        var top = new Clothing
        {
            Name = "白衬衫",
            Type = ClothingType.Top,
            ImagePath = "shared.png"
        };
        var dress = new Clothing
        {
            Name = "黑裙子",
            Type = ClothingType.Dress,
            ImagePath = "shared.png"
        };
        var shoes = new Clothing
        {
            Name = "乐福鞋",
            Type = ClothingType.Shoes,
            ImagePath = "shoes.png"
        };

        var clothingRepository = new FakeClothingRepository([top, dress, shoes]);
        var outfitRepository = new FakeOutfitRepository();
        var wornRecordRepository = new FakeOutfitWornRecordRepository();
        var imageStorage = new FakeImageStorageService();
        var useCase = new ClearWardrobeByTypes(clothingRepository, outfitRepository, wornRecordRepository, imageStorage);

        var result = await useCase.ExecuteAsync(new BatchWardrobeClearRequest([ClothingType.Top, ClothingType.Dress]));

        Assert.Equal(2, result.DeletedCount);
        Assert.Equal([top.Id, dress.Id], clothingRepository.DeletedIds);
        Assert.Equal(1, outfitRepository.DeleteEmptyOutfitsCallCount);
        Assert.Equal(["shared.png"], imageStorage.DeletedImagePaths);
        Assert.Single(clothingRepository.StoredClothes);
        Assert.Equal(ClothingType.Shoes, clothingRepository.StoredClothes[0].Type);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutTypes_Throws()
    {
        var useCase = new ClearWardrobeByTypes(
            new FakeClothingRepository([]),
            new FakeOutfitRepository(),
            new FakeOutfitWornRecordRepository(),
            new FakeImageStorageService());

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(new BatchWardrobeClearRequest([])));
    }

    [Fact]
    public async Task ExecuteAsync_WhenImageReferencedByHistory_DoesNotDeleteImage()
    {
        var skirt = new Clothing
        {
            Name = "黑色半裙",
            Type = ClothingType.Skirt,
            ImagePath = "skirt.png"
        };
        var clothingRepository = new FakeClothingRepository([skirt]);
        var outfitRepository = new FakeOutfitRepository();
        var wornRecordRepository = new FakeOutfitWornRecordRepository();
        wornRecordRepository.ReferencedImagePaths.Add("skirt.png");
        var imageStorage = new FakeImageStorageService();
        var useCase = new ClearWardrobeByTypes(clothingRepository, outfitRepository, wornRecordRepository, imageStorage);

        await useCase.ExecuteAsync(new BatchWardrobeClearRequest([ClothingType.Skirt]));

        Assert.Empty(imageStorage.DeletedImagePaths);
    }

    private sealed class FakeClothingRepository : IClothingRepository
    {
        private readonly List<Clothing> _storedClothes;

        public FakeClothingRepository(IEnumerable<Clothing> clothes)
        {
            _storedClothes = clothes.ToList();
        }

        public List<Guid> DeletedIds { get; } = [];
        public IReadOnlyList<Clothing> StoredClothes => _storedClothes;

        public Task<IEnumerable<Clothing>> GetAllAsync() => Task.FromResult(_storedClothes.AsEnumerable());
        public Task<Clothing?> GetByIdAsync(Guid id) => Task.FromResult(_storedClothes.FirstOrDefault(clothing => clothing.Id == id));
        public Task AddAsync(Clothing entity) => throw new NotImplementedException();
        public Task UpdateAsync(Clothing entity) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type) => Task.FromResult(_storedClothes.Where(clothing => clothing.Type == type).AsEnumerable());

        public Task<IEnumerable<Clothing>> GetByTypesAsync(IEnumerable<ClothingType> types)
        {
            var selectedTypes = types.ToHashSet();
            return Task.FromResult(_storedClothes.Where(clothing => selectedTypes.Contains(clothing.Type)).AsEnumerable());
        }

        public Task AddRangeAsync(IEnumerable<Clothing> clothes) => throw new NotImplementedException();

        public Task DeleteRangeAsync(IEnumerable<Guid> ids)
        {
            var selectedIds = ids.ToHashSet();
            DeletedIds.AddRange(selectedIds);
            _storedClothes.RemoveAll(clothing => selectedIds.Contains(clothing.Id));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOutfitRepository : IOutfitRepository
    {
        public int DeleteEmptyOutfitsCallCount { get; private set; }

        public Task<IEnumerable<Outfit>> GetAllAsync() => throw new NotImplementedException();
        public Task<Outfit?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task<Outfit?> GetByIdForUpdateAsync(Guid id) => throw new NotImplementedException();
        public Task AddAsync(Outfit entity) => throw new NotImplementedException();
        public Task UpdateAsync(Outfit entity) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId) => Task.FromResult(Enumerable.Empty<Outfit>());

        public Task DeleteEmptyOutfitsAsync()
        {
            DeleteEmptyOutfitsCallCount++;
            return Task.CompletedTask;
        }

        public Task<List<OutfitUpdateResult>> DeleteInvalidOutfitsAsync(Guid excludedClothingId) => Task.FromResult(new List<OutfitUpdateResult>());
    }

    private sealed class FakeOutfitWornRecordRepository : IOutfitWornRecordRepository
    {
        public HashSet<string> ReferencedImagePaths { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<IEnumerable<OutfitWornRecord>> GetAllAsync() => throw new NotImplementedException();
        public Task<OutfitWornRecord?> GetByIdAsync(Guid id) => throw new NotImplementedException();
        public Task AddAsync(OutfitWornRecord entity) => throw new NotImplementedException();
        public Task UpdateAsync(OutfitWornRecord entity) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task<int> DeleteAllAsync() => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end) => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId) => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count) => throw new NotImplementedException();
        public Task<bool> IsImageReferencedBySnapshotAsync(string imagePath) =>
            Task.FromResult(ReferencedImagePaths.Contains(imagePath));
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public List<string> DeletedImagePaths { get; } = [];

        public Task<string> SaveImageAsync(string sourcePath) => throw new NotImplementedException();
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
        public string GetOriginalsDirectory() => "";
        public string GetDisplayDirectory() => "";
        public string GetThumbnailsDirectory() => "";
        public Task MigrateGlobalImagesAsync() => Task.CompletedTask;
    }
}
