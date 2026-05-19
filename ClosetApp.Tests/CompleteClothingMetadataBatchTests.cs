using ClosetApp.Application.DTOs;
using ClosetApp.Application.UseCases.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using Xunit;

namespace ClosetApp.Tests;

public class CompleteClothingMetadataBatchTests
{
    [Fact]
    public async Task ExecuteAsync_FillsOnlyMissingMetadata_AndAddsMissingTags()
    {
        var tagToAdd = Guid.NewGuid();
        var existingTag = Guid.NewGuid();
        var incomplete = new Clothing
        {
            Name = "未命名",
            Type = ClothingType.Unspecified,
            Season = Season.Unspecified,
            Brand = null,
            Color = null
        };
        incomplete.ClothingTags.Add(new ClothingTag { ClothingId = incomplete.Id, TagId = existingTag });

        var complete = new Clothing
        {
            Name = "Ready Coat",
            Type = ClothingType.Outerwear,
            Season = Season.Winter,
            Brand = "Uniqlo",
            Color = "Black"
        };

        var repository = new FakeClothingRepository([incomplete, complete]);
        var useCase = new CompleteClothingMetadataBatch(repository);

        var result = await useCase.ExecuteAsync(new BatchClothingCompletionRequest(
            [incomplete.Id, complete.Id],
            ClothingType.Outerwear,
            Season.Winter,
            " Black ",
            " Uniqlo ",
            [existingTag, tagToAdd]));

        Assert.Equal(2, result.UpdatedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(ClothingType.Outerwear, incomplete.Type);
        Assert.Equal(Season.Winter, incomplete.Season);
        Assert.Equal("Black", incomplete.Color);
        Assert.Equal("Uniqlo", incomplete.Brand);
        Assert.Equal(2, incomplete.ClothingTags.Count);
        Assert.Equal(2, repository.UpdatedClothingIds.Count);
        Assert.Contains(incomplete.Id, repository.UpdatedClothingIds);
        Assert.Contains(complete.Id, repository.UpdatedClothingIds);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutRequestedChanges_Throws()
    {
        var clothing = new Clothing { Name = "Soft Knit" };
        var repository = new FakeClothingRepository([clothing]);
        var useCase = new CompleteClothingMetadataBatch(repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.ExecuteAsync(new BatchClothingCompletionRequest(
            [clothing.Id],
            null,
            null,
            null,
            null,
            [])));
    }

    private sealed class FakeClothingRepository : IClothingRepository
    {
        private readonly Dictionary<Guid, Clothing> _clothes;

        public FakeClothingRepository(IEnumerable<Clothing> clothes)
        {
            _clothes = clothes.ToDictionary(clothing => clothing.Id);
        }

        public List<Guid> UpdatedClothingIds { get; } = [];

        public Task<IEnumerable<Clothing>> GetAllAsync() => Task.FromResult(_clothes.Values.AsEnumerable());
        public Task<Clothing?> GetByIdAsync(Guid id) => Task.FromResult(_clothes.TryGetValue(id, out var clothing) ? clothing : null);
        public Task AddAsync(Clothing entity) => throw new NotImplementedException();
        public Task UpdateAsync(Clothing entity)
        {
            _clothes[entity.Id] = entity;
            UpdatedClothingIds.Add(entity.Id);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Guid id) => throw new NotImplementedException();
        public Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<Clothing> clothes) => throw new NotImplementedException();
    }
}
