using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class DeleteOutfitGeneratedImageTests
{
    [Fact]
    public async Task ExecuteAsync_WhenDeletingPrimaryImage_PromotesLatestRemainingImage()
    {
        var outfitId = Guid.NewGuid();
        var deletedImage = new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            OutfitId = outfitId,
            ResultImagePath = "deleted.png",
            IsPrimary = true,
            Status = "Succeeded",
            CreatedAt = DateTime.Now.AddMinutes(-30)
        };
        var remainingImage = new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            OutfitId = outfitId,
            ResultImagePath = "remaining.png",
            IsPrimary = false,
            Status = "Succeeded",
            CreatedAt = DateTime.Now.AddMinutes(-5)
        };
        var repository = new FakeOutfitGeneratedImageRepository(deletedImage, remainingImage);
        var deletedPaths = new List<string?>();
        var useCase = new DeleteOutfitGeneratedImage(
            repository,
            new FakeAiAssetStorageService(path => deletedPaths.Add(path)));

        await useCase.ExecuteAsync(deletedImage.Id);

        Assert.DoesNotContain(repository.Images, image => image.Id == deletedImage.Id);
        Assert.Single(deletedPaths, "deleted.png");
        Assert.True(repository.Images.Single().IsPrimary);
    }

    private sealed class FakeOutfitGeneratedImageRepository : IOutfitGeneratedImageRepository
    {
        public List<OutfitGeneratedImage> Images { get; }

        public FakeOutfitGeneratedImageRepository(params OutfitGeneratedImage[] images)
        {
            Images = images.ToList();
        }

        public Task<IEnumerable<OutfitGeneratedImage>> GetAllAsync() => Task.FromResult<IEnumerable<OutfitGeneratedImage>>(Images);
        public Task<OutfitGeneratedImage?> GetByIdAsync(Guid id) => Task.FromResult(Images.FirstOrDefault(image => image.Id == id));
        public Task AddAsync(OutfitGeneratedImage entity)
        {
            Images.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(OutfitGeneratedImage entity)
        {
            var index = Images.FindIndex(image => image.Id == entity.Id);
            if (index >= 0)
                Images[index] = entity;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id)
        {
            Images.RemoveAll(image => image.Id == id);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<OutfitGeneratedImage>> GetByOutfitIdAsync(Guid outfitId)
        {
            return Task.FromResult<IReadOnlyList<OutfitGeneratedImage>>(Images.Where(image => image.OutfitId == outfitId).OrderByDescending(image => image.CreatedAt).ToList());
        }

        public Task<OutfitGeneratedImage?> GetPrimaryByOutfitIdAsync(Guid outfitId)
        {
            return Task.FromResult(Images.FirstOrDefault(image => image.OutfitId == outfitId && image.IsPrimary));
        }

        public Task ClearPrimaryAsync(Guid outfitId, Guid? excludingId = null)
        {
            foreach (var image in Images.Where(image => image.OutfitId == outfitId && (!excludingId.HasValue || image.Id != excludingId.Value)))
                image.IsPrimary = false;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAiAssetStorageService : IAiAssetStorageService
    {
        private readonly Action<string?> _onDelete;

        public FakeAiAssetStorageService(Action<string?> onDelete)
        {
            _onDelete = onDelete;
        }

        public Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName) => throw new NotImplementedException();
        public Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType) => throw new NotImplementedException();
        public Task RestoreProfileReferenceImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task RestoreGeneratedImageAsync(string sourcePath, string storedFileName) => throw new NotImplementedException();
        public Task TryDeleteProfileReferenceImageAsync(string? imagePath) => Task.CompletedTask;
        public Task TryDeleteGeneratedImageAsync(string? imagePath)
        {
            _onDelete(imagePath);
            return Task.CompletedTask;
        }

        public string GetProfileReferenceFullPath(string relativePath) => relativePath;
        public string GetGeneratedImageFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetGeneratedImageAssetFullPaths(string relativePath) => [relativePath];
    }
}
