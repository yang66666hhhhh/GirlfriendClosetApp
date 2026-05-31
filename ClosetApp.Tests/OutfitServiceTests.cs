using ClosetApp.Application.Services;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Images;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using System.Text.Json;
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

    [Fact]
    public async Task AnalyzeWornRecordImageHealthAsync_WithMissingSnapshotImage_ReturnsCounts()
    {
        var wornRecordRepository = new FakeOutfitWornRecordRepository
        {
            Records =
            [
                new OutfitWornRecord
                {
                    Id = Guid.NewGuid(),
                    WornDate = new DateTime(2026, 5, 30),
                    OutfitNameSnapshot = "约会搭配",
                    IsSnapshotComplete = true,
                    ClothingDetailsSnapshot = JsonSerializer.Serialize(new[]
                    {
                        new ClothingSnapshotDto
                        {
                            Id = Guid.NewGuid(),
                            Name = "黑色半裙",
                            ImagePath = "missing-skirt.jpg",
                            Type = nameof(ClothingType.Skirt)
                        }
                    })
                }
            ]
        };
        var service = new OutfitService(
            new FakeOutfitRepository(),
            wornRecordRepository,
            new FakeFavoriteRepository(),
            new FakeImageAssetResolver());

        var result = await service.AnalyzeWornRecordImageHealthAsync();

        Assert.Equal(1, result.RecordCount);
        Assert.Equal(1, result.SnapshotClothingCount);
        Assert.Equal(1, result.MissingImageCount);
        Assert.Equal(1, result.RecordsWithMissingImages);
        var missingRecord = Assert.Single(result.MissingRecordItems);
        Assert.Equal("约会搭配", missingRecord.OutfitName);
        Assert.Equal(new DateTime(2026, 5, 30), missingRecord.WornDate);
        Assert.Equal(1, missingRecord.MissingImageCount);
    }

    [Fact]
    public async Task RepairWornRecordSnapshotImageAsync_UpdatesTargetSnapshotImage()
    {
        var clothingId = Guid.NewGuid();
        var record = new OutfitWornRecord
        {
            Id = Guid.NewGuid(),
            IsSnapshotComplete = true,
            ClothingDetailsSnapshot = JsonSerializer.Serialize(new[]
            {
                new ClothingSnapshotDto
                {
                    Id = clothingId,
                    Name = "黑色半裙",
                    ImagePath = "missing-skirt.jpg",
                    Type = nameof(ClothingType.Skirt)
                }
            })
        };
        var wornRecordRepository = new FakeOutfitWornRecordRepository
        {
            Records = [record]
        };
        var service = new OutfitService(
            new FakeOutfitRepository(),
            wornRecordRepository,
            new FakeFavoriteRepository(),
            new FakeImageAssetResolver());

        await service.RepairWornRecordSnapshotImageAsync(record.Id, clothingId, "new-skirt.jpg");

        var snapshotClothes = JsonSerializer.Deserialize<List<ClothingSnapshotDto>>(record.ClothingDetailsSnapshot!);
        Assert.Equal("new-skirt.jpg", Assert.Single(snapshotClothes!).ImagePath);
        Assert.Equal(record.Id, wornRecordRepository.UpdatedRecordId);
    }

    [Fact]
    public async Task RepairWornRecordSnapshotImageAsync_WithUnknownSnapshotClothing_Throws()
    {
        var record = new OutfitWornRecord
        {
            Id = Guid.NewGuid(),
            IsSnapshotComplete = true,
            ClothingDetailsSnapshot = JsonSerializer.Serialize(new[]
            {
                new ClothingSnapshotDto
                {
                    Id = Guid.Empty,
                    Name = "旧快照单品",
                    ImagePath = "missing.jpg",
                    Type = nameof(ClothingType.Skirt)
                }
            })
        };
        var service = new OutfitService(
            new FakeOutfitRepository(),
            new FakeOutfitWornRecordRepository { Records = [record] },
            new FakeFavoriteRepository(),
            new FakeImageAssetResolver());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RepairWornRecordSnapshotImageAsync(record.Id, Guid.NewGuid(), "new.jpg"));
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
        public List<OutfitWornRecord> Records { get; init; } = [];
        public bool IsImageReferencedBySnapshot { get; init; }
        public string? CheckedImagePath { get; private set; }
        public Guid? UpdatedRecordId { get; private set; }

        public Task<IEnumerable<OutfitWornRecord>> GetAllAsync() => Task.FromResult(Records.AsEnumerable());
        public Task<OutfitWornRecord?> GetByIdAsync(Guid id) => Task.FromResult(Records.FirstOrDefault(record => record.Id == id));
        public Task AddAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task UpdateAsync(OutfitWornRecord entity)
        {
            UpdatedRecordId = entity.Id;
            return Task.CompletedTask;
        }
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

    private sealed class FakeImageAssetResolver : IImageAssetResolver
    {
        public ImageAsset Resolve(string? imagePath)
        {
            return string.Equals(imagePath, "available.jpg", StringComparison.OrdinalIgnoreCase)
                ? new ImageAsset(imagePath, imagePath, imagePath, null)
                : new ImageAsset(imagePath, null, null, null);
        }

        public string? ResolvePath(string? imagePath, ImageVariant variant)
        {
            return Resolve(imagePath).HasImage ? imagePath : null;
        }
    }
}
