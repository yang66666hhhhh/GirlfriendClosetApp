using ClosetApp.Application.Services;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitServiceTests
{
    [Fact]
    public async Task AddOutfitAsync_UsesDefaultNameWhenNameIsBlank()
    {
        var repository = new FakeOutfitRepository();
        var service = new OutfitService(
            repository,
            new FakeOutfitWornRecordRepository(),
            new FakeFavoriteRepository());

        var outfit = new Outfit
        {
            Name = "   ",
            Scene = OutfitScene.Casual,
            Season = Season.AllSeason
        };

        var saved = await service.AddOutfitAsync(outfit);

        Assert.Equal("未命名", saved.Name);
        Assert.Equal("未命名", Assert.Single(repository.AddedOutfits).Name);
    }

    [Fact]
    public async Task DeleteClothingAsync_WhenImageReferencedByHistory_PreservesImage()
    {
        var clothingId = Guid.NewGuid();
        var wornRecordRepository = new FakeOutfitWornRecordRepository
        {
            IsImageReferencedBySnapshot = true
        };
        var clothingRepository = new FakeClothingRepository(new Clothing
        {
            Id = clothingId,
            Name = "黑色半裙",
            Type = ClothingType.Skirt,
            ImagePath = "skirt.jpg"
        });
        var service = new ClothingService(
            clothingRepository,
            new FakeOutfitRepository(),
            wornRecordRepository);

        var result = await service.DeleteClothingAsync(clothingId);

        Assert.True(result.PreserveDeletedImageForHistory);
        Assert.Equal(clothingId, clothingRepository.DeletedId);
        Assert.Equal("skirt.jpg", wornRecordRepository.CheckedImagePath);
    }

    private sealed class FakeClothingRepository : IClothingRepository
    {
        private readonly Clothing _clothing;

        public FakeClothingRepository(Clothing clothing)
        {
            _clothing = clothing;
        }

        public Guid? DeletedId { get; private set; }

        public Task<IEnumerable<Clothing>> GetAllAsync() => Task.FromResult<IEnumerable<Clothing>>([_clothing]);
        public Task<Clothing?> GetByIdAsync(Guid id) => Task.FromResult(id == _clothing.Id ? _clothing : null);
        public Task AddAsync(Clothing entity) => throw new NotImplementedException();
        public Task UpdateAsync(Clothing entity) => throw new NotImplementedException();
        public Task DeleteAsync(Guid id)
        {
            DeletedId = id;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type) => throw new NotImplementedException();
        public Task<IEnumerable<Clothing>> GetByTypesAsync(IEnumerable<ClothingType> types) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<Clothing> clothes) => throw new NotImplementedException();
        public Task DeleteRangeAsync(IEnumerable<Guid> ids) => throw new NotImplementedException();
    }

    private sealed class FakeOutfitRepository : IOutfitRepository
    {
        public List<Outfit> AddedOutfits { get; } = [];

        public Task<IEnumerable<Outfit>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Outfit>());
        public Task<Outfit?> GetByIdAsync(Guid id) => Task.FromResult<Outfit?>(null);
        public Task<Outfit?> GetByIdForUpdateAsync(Guid id) => Task.FromResult<Outfit?>(null);

        public Task AddAsync(Outfit entity)
        {
            AddedOutfits.Add(entity);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Outfit entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene) => Task.FromResult(Enumerable.Empty<Outfit>());
        public Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season) => Task.FromResult(Enumerable.Empty<Outfit>());
        public Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count) => Task.FromResult(Enumerable.Empty<Outfit>());
        public Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId) => Task.FromResult(Enumerable.Empty<Outfit>());
        public Task DeleteEmptyOutfitsAsync() => Task.CompletedTask;
        public Task<List<OutfitUpdateResult>> DeleteInvalidOutfitsAsync(Guid excludedClothingId) => Task.FromResult(new List<OutfitUpdateResult>());
    }

    private sealed class FakeOutfitWornRecordRepository : IOutfitWornRecordRepository
    {
        public bool IsImageReferencedBySnapshot { get; init; }
        public string? CheckedImagePath { get; private set; }

        public Task<IEnumerable<OutfitWornRecord>> GetAllAsync() => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<OutfitWornRecord?> GetByIdAsync(Guid id) => Task.FromResult<OutfitWornRecord?>(null);
        public Task AddAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task UpdateAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end) => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId) => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count) => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<bool> IsImageReferencedBySnapshotAsync(string imagePath)
        {
            CheckedImagePath = imagePath;
            return Task.FromResult(IsImageReferencedBySnapshot);
        }
    }

    private sealed class FakeFavoriteRepository : IFavoriteRepository
    {
        public Task<IEnumerable<Favorite>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Favorite>());
        public Task<Favorite?> GetByIdAsync(Guid id) => Task.FromResult<Favorite?>(null);
        public Task AddAsync(Favorite entity) => Task.CompletedTask;
        public Task UpdateAsync(Favorite entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<Favorite>> GetByOutfitIdAsync(Guid outfitId) => Task.FromResult(Enumerable.Empty<Favorite>());
    }
}
