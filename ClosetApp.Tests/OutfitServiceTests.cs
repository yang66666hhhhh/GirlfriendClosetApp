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

    private sealed class FakeOutfitRepository : IOutfitRepository
    {
        public List<Outfit> AddedOutfits { get; } = [];

        public Task<IEnumerable<Outfit>> GetAllAsync() => Task.FromResult(Enumerable.Empty<Outfit>());
        public Task<Outfit?> GetByIdAsync(Guid id) => Task.FromResult<Outfit?>(null);

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
        public Task DeleteEmptyOutfitsAsync() => Task.CompletedTask;
    }

    private sealed class FakeOutfitWornRecordRepository : IOutfitWornRecordRepository
    {
        public Task<IEnumerable<OutfitWornRecord>> GetAllAsync() => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<OutfitWornRecord?> GetByIdAsync(Guid id) => Task.FromResult<OutfitWornRecord?>(null);
        public Task AddAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task UpdateAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end) => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId) => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
        public Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count) => Task.FromResult(Enumerable.Empty<OutfitWornRecord>());
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
