using System.IO;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class SaveUploadedOutfitGeneratedImageTests
{
    [Fact]
    public async Task ExecuteAsync_WithValidImage_SavesAsGeneratedHistory()
    {
        var tempFile = Path.GetTempFileName();
        await File.WriteAllBytesAsync(tempFile, [1, 2, 3, 4]);

        try
        {
            var outfit = new Outfit
            {
                Id = Guid.NewGuid(),
                Name = "测试搭配",
                Scene = OutfitScene.Casual,
                Season = Season.Spring
            };

            var repository = new FakeOutfitGeneratedImageRepository();
            var assetStorage = new FakeAiAssetStorageService();
            var useCase = new SaveUploadedOutfitGeneratedImage(
                new FakeOutfitService(outfit),
                repository,
                assetStorage);

            var result = await useCase.ExecuteAsync(new SaveUploadedOutfitGeneratedImageRequest(outfit.Id, tempFile));

            Assert.Single(repository.Images);
            Assert.Equal("Manual Upload", repository.Images[0].ProviderKind);
            Assert.True(repository.Images[0].IsPrimary);
            Assert.Equal("stored-image.png", result.ResultImagePath);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    private sealed class FakeOutfitService : IOutfitService
    {
        private readonly Outfit _outfit;

        public FakeOutfitService(Outfit outfit)
        {
            _outfit = outfit;
        }

        public Task<IEnumerable<Outfit>> GetAllOutfitsAsync() => Task.FromResult<IEnumerable<Outfit>>([_outfit]);
        public Task<Outfit?> GetOutfitByIdAsync(Guid id) => Task.FromResult(id == _outfit.Id ? _outfit : null);
        public Task<Outfit> AddOutfitAsync(Outfit outfit) => throw new NotImplementedException();
        public Task UpdateOutfitAsync(Outfit outfit) => throw new NotImplementedException();
        public Task DeleteOutfitAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene) => throw new NotImplementedException();
        public Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count) => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetRecentWornRecordsAsync(int count) => throw new NotImplementedException();
        public Task<IEnumerable<OutfitWornRecord>> GetWornRecordsAsync(DateTime start, DateTime end) => throw new NotImplementedException();
        public Task RecordWornDateAsync(Guid outfitId, DateTime date) => throw new NotImplementedException();
        public Task<WornRecordImageHealthDto> AnalyzeWornRecordImageHealthAsync() => throw new NotImplementedException();
        public Task RepairWornRecordSnapshotImageAsync(Guid recordId, Guid clothingId, string imagePath) => throw new NotImplementedException();
        public Task DeleteWornRecordAsync(Guid recordId) => throw new NotImplementedException();
        public Task<int> ClearWornHistoryAsync() => Task.FromResult(0);
        public Task<bool> ToggleFavoriteAsync(Guid outfitId) => throw new NotImplementedException();
        public Task<IReadOnlyList<OutfitGeneratedImage>> GetGeneratedImagesAsync(Guid outfitId) => Task.FromResult<IReadOnlyList<OutfitGeneratedImage>>([]);
    }

    private sealed class FakeOutfitGeneratedImageRepository : IOutfitGeneratedImageRepository
    {
        public List<OutfitGeneratedImage> Images { get; } = [];

        public Task<IEnumerable<OutfitGeneratedImage>> GetAllAsync() => Task.FromResult<IEnumerable<OutfitGeneratedImage>>(Images);
        public Task<OutfitGeneratedImage?> GetByIdAsync(Guid id) => Task.FromResult(Images.FirstOrDefault(image => image.Id == id));
        public Task AddAsync(OutfitGeneratedImage entity)
        {
            Images.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OutfitGeneratedImage entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IReadOnlyList<OutfitGeneratedImage>> GetByOutfitIdAsync(Guid outfitId) => Task.FromResult<IReadOnlyList<OutfitGeneratedImage>>(Images.Where(image => image.OutfitId == outfitId).ToList());
        public Task<OutfitGeneratedImage?> GetPrimaryByOutfitIdAsync(Guid outfitId) => Task.FromResult(Images.FirstOrDefault(image => image.OutfitId == outfitId && image.IsPrimary));
        public Task ClearPrimaryAsync(Guid outfitId, Guid? excludingId = null) => Task.CompletedTask;
    }

    private sealed class FakeAiAssetStorageService : IAiAssetStorageService
    {
        public Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName, Guid? userId = null) => throw new NotImplementedException();
        public Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType) => Task.FromResult("stored-image.png");
        public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName, Guid? userId = null) => throw new NotImplementedException();
        public Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task TryDeleteProfileReferenceImageAsync(string? imagePath, Guid? userId = null) => Task.CompletedTask;
        public Task TryDeleteGeneratedImageAsync(string? imagePath) => Task.CompletedTask;
        public string GetProfileReferenceFullPath(string relativePath, Guid? userId = null) => relativePath;
        public string GetGeneratedImageFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath) => [relativePath];
        public string GetAiRendersDisplayDirectory() => "";
        public string GetAiRendersThumbnailsDirectory() => "";
        public Task MigrateGlobalAiAssetsAsync() => Task.CompletedTask;
    }
}
