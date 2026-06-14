using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Clothing;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using ClosetApp.UI.ViewModels;
using Xunit;

namespace ClosetApp.Tests;

public class WardrobeViewModelTests
{
    [Fact]
    public async Task LoadClothesAsync_DefaultDisplayWindow_ShowsFirstPageOnly()
    {
        var clothes = Enumerable.Range(1, 25)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, index)))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();

        Assert.Equal(25, viewModel.FilteredClothes.Count);
        Assert.Equal(20, viewModel.DisplayedClothes.Count);
        Assert.Equal("Clothing 25", viewModel.DisplayedClothes[0].Name);
        Assert.True(viewModel.HasMoreClothes);
    }

    [Fact]
    public async Task LoadMoreClothes_ExpandsDisplayedWindowWithoutRequeryingState()
    {
        var clothes = Enumerable.Range(1, 25)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, index)))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();
        viewModel.LoadMoreClothes();

        Assert.Equal(25, viewModel.DisplayedClothes.Count);
        Assert.False(viewModel.HasMoreClothes);
        Assert.Equal("Clothing 1", viewModel.DisplayedClothes[^1].Name);
    }

    [Fact]
    public async Task SearchText_WhenFilterShrinksResult_RefreshesDisplayedWindowToFilteredSet()
    {
        var clothes = Enumerable.Range(1, 25)
            .Select(index => CreateClothing(
                index <= 3 ? $"Match {index}" : $"Clothing {index}",
                createdAt: new DateTime(2026, 5, index)))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();
        viewModel.LoadMoreClothes();
        viewModel.SearchText = "Match";

        Assert.Equal(3, viewModel.FilteredClothes.Count);
        Assert.Equal(3, viewModel.DisplayedClothes.Count);
        Assert.All(viewModel.DisplayedClothes, clothing => Assert.Contains("Match", clothing.Name));
        Assert.False(viewModel.HasMoreClothes);
    }

    [Fact]
    public async Task TryPrefetchMoreClothes_WhenNearBottom_ExpandsVisibleWindow()
    {
        var clothes = Enumerable.Range(1, 45)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, Math.Min(index, 28))))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();

        var prefetched = viewModel.TryPrefetchMoreClothes(
            verticalOffset: 1800,
            viewportHeight: 900,
            extentHeight: 3200);

        Assert.True(prefetched);
        Assert.Equal(40, viewModel.DisplayedClothes.Count);
        Assert.True(viewModel.HasMoreClothes);
    }

    [Fact]
    public async Task TryPrefetchMoreClothes_WhenFarFromBottom_DoesNotExpandWindow()
    {
        var clothes = Enumerable.Range(1, 45)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, Math.Min(index, 28))))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();

        var prefetched = viewModel.TryPrefetchMoreClothes(
            verticalOffset: 200,
            viewportHeight: 900,
            extentHeight: 4200);

        Assert.False(prefetched);
        Assert.Equal(20, viewModel.DisplayedClothes.Count);
        Assert.True(viewModel.HasMoreClothes);
    }

    [Fact]
    public async Task TryPrefetchMoreClothes_RepeatedNearBottomCall_DoesNotConsumeMultiplePages()
    {
        var clothes = Enumerable.Range(1, 65)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, Math.Min(index, 28))))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();

        var firstPrefetch = viewModel.TryPrefetchMoreClothes(
            verticalOffset: 1800,
            viewportHeight: 900,
            extentHeight: 3200);
        var secondPrefetch = viewModel.TryPrefetchMoreClothes(
            verticalOffset: 1800,
            viewportHeight: 900,
            extentHeight: 3200);

        Assert.True(firstPrefetch);
        Assert.False(secondPrefetch);
        Assert.Equal(40, viewModel.DisplayedClothes.Count);
        Assert.True(viewModel.HasMoreClothes);
    }

    [Fact]
    public async Task TryPrefetchMoreClothes_AutoPrefetchStopsAtConfiguredWindowLimit()
    {
        var clothes = Enumerable.Range(1, 120)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, Math.Min(index, 28))))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();

        viewModel.LoadMoreClothes();
        var prefetched = viewModel.TryPrefetchMoreClothes(2600, 900, 4200);

        Assert.True(prefetched);
        Assert.Equal(60, viewModel.DisplayedClothes.Count);
        Assert.True(viewModel.HasMoreClothes);
    }

    [Fact]
    public async Task TryPrefetchMoreClothes_WhenAutoPrefetchWindowIsAtLimit_DoesNothing()
    {
        var clothes = Enumerable.Range(1, 120)
            .Select(index => CreateClothing($"Clothing {index}", createdAt: new DateTime(2026, 5, Math.Min(index, 28))))
            .ToList();
        var viewModel = CreateViewModel(clothes);

        await viewModel.LoadClothesAsync();
        viewModel.LoadMoreClothes();
        viewModel.LoadMoreClothes();

        viewModel.TryPrefetchMoreClothes(200, 900, 4200);
        var prefetched = viewModel.TryPrefetchMoreClothes(
            verticalOffset: 2600,
            viewportHeight: 900,
            extentHeight: 4200);

        Assert.False(prefetched);
        Assert.Equal(60, viewModel.DisplayedClothes.Count);
        Assert.True(viewModel.HasMoreClothes);
    }

    private static WardrobeViewModel CreateViewModel(IEnumerable<Clothing> clothes)
    {
        return new WardrobeViewModel(
            new FakeClothingService(clothes),
            new FakeTagService(),
            new FakeImageStorageService(),
            new CompleteClothingMetadataBatch(new StubClothingRepository()),
            new ClearWardrobeByTypes(new StubClothingRepository(), new StubOutfitRepository(), new StubOutfitWornRecordRepository(), new FakeImageStorageService()),
            new ImportClothesFromImages(new StubClothingRepository(), new FakeImageStorageService()));
    }

    private static Clothing CreateClothing(string name, DateTime createdAt)
    {
        return new Clothing
        {
            Name = name,
            Type = ClothingType.Top,
            GarmentType = GarmentType.TShirt,
            Season = Season.AllSeason,
            CreatedAt = createdAt
        };
    }

    private sealed class FakeClothingService : IClothingService
    {
        private readonly List<Clothing> _clothes;

        public FakeClothingService(IEnumerable<Clothing> clothes)
        {
            _clothes = clothes.ToList();
        }

        public Task<IEnumerable<Clothing>> GetAllClothesAsync() => Task.FromResult<IEnumerable<Clothing>>(_clothes);
        public Task<Clothing?> GetClothingByIdAsync(Guid id) => Task.FromResult(_clothes.FirstOrDefault(c => c.Id == id));
        public Task<Clothing> AddClothingAsync(Clothing clothing)
        {
            _clothes.Add(clothing);
            return Task.FromResult(clothing);
        }

        public Task AddClothesAsync(IEnumerable<Clothing> clothes)
        {
            _clothes.AddRange(clothes);
            return Task.CompletedTask;
        }

        public Task UpdateClothingAsync(Clothing clothing) => Task.CompletedTask;

        public Task<ClothingDeleteResult> DeleteClothingAsync(Guid id)
        {
            _clothes.RemoveAll(clothing => clothing.Id == id);
            return Task.FromResult(new ClothingDeleteResult { Success = true });
        }

        public Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId)
            => Task.FromResult<IEnumerable<Outfit>>([]);

        public Task<IEnumerable<Clothing>> GetClothesByTypeAsync(ClothingType type)
            => Task.FromResult<IEnumerable<Clothing>>(_clothes.Where(clothing => clothing.Type == type).ToList());

        public Task<IEnumerable<Clothing>> SearchClothesAsync(string keyword)
            => Task.FromResult<IEnumerable<Clothing>>(_clothes.Where(clothing => clothing.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList());
    }

    private sealed class FakeTagService : ITagService
    {
        public Task<IEnumerable<Tag>> GetAllTagsAsync() => Task.FromResult<IEnumerable<Tag>>([]);
        public Task<IEnumerable<Tag>> GetStyleTagsAsync() => Task.FromResult<IEnumerable<Tag>>([]);
        public Task<IEnumerable<Tag>> GetTagsByCategoryAsync(TagCategory category) => Task.FromResult<IEnumerable<Tag>>([]);
        public Task<Tag> AddTagAsync(Tag tag) => Task.FromResult(tag);
        public Task UpdateTagAsync(Tag tag) => Task.CompletedTask;
        public Task DeleteTagAsync(Guid id) => Task.CompletedTask;
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public Task<string> SaveImageAsync(string sourcePath) => Task.FromResult(sourcePath);
        public Task<string> SaveThumbnailAsync(string sourcePath, int maxSize = 200) => Task.FromResult(sourcePath);
        public Task<bool> EnsureThumbnailAsync(string imagePath, int maxSize = 200) => Task.FromResult(true);
        public Task<bool> EnsureDisplayAsync(string imagePath, int maxWidth = 900) => Task.FromResult(true);
        public Task RestoreImageAsync(string sourcePath, string storedFileName) => Task.CompletedTask;
        public Task DeleteImageAsync(string imagePath) => Task.CompletedTask;
        public Task DeleteImageWithThumbnailAsync(string imagePath) => Task.CompletedTask;
        public Task TryDeleteImageAsync(string? imagePath) => Task.CompletedTask;
        public string GetImageFullPath(string relativePath) => relativePath;
        public string GetDisplayFullPath(string relativePath) => relativePath;
        public string GetThumbnailFullPath(string relativePath) => relativePath;
        public IReadOnlyList<string> GetOriginalImageFullPaths() => [];
        public IReadOnlyList<string> GetImageAssetFullPaths(string relativePath) => [];
    }

    private sealed class StubClothingRepository : IClothingRepository
    {
        public Task<IEnumerable<Clothing>> GetAllAsync() => Task.FromResult<IEnumerable<Clothing>>([]);
        public Task<Clothing?> GetByIdAsync(Guid id) => Task.FromResult<Clothing?>(null);
        public Task AddAsync(Clothing entity) => Task.CompletedTask;
        public Task UpdateAsync(Clothing entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type) => Task.FromResult<IEnumerable<Clothing>>([]);
        public Task<IEnumerable<Clothing>> GetByTypesAsync(IEnumerable<ClothingType> types) => Task.FromResult<IEnumerable<Clothing>>([]);
        public Task AddRangeAsync(IEnumerable<Clothing> clothes) => Task.CompletedTask;
        public Task DeleteRangeAsync(IEnumerable<Guid> ids) => Task.CompletedTask;
    }

    private sealed class StubOutfitRepository : IOutfitRepository
    {
        public Task<IEnumerable<Outfit>> GetAllAsync() => Task.FromResult<IEnumerable<Outfit>>([]);
        public Task<Outfit?> GetByIdAsync(Guid id) => Task.FromResult<Outfit?>(null);
        public Task<Outfit?> GetByIdForUpdateAsync(Guid id) => Task.FromResult<Outfit?>(null);
        public Task AddAsync(Outfit entity) => Task.CompletedTask;
        public Task UpdateAsync(Outfit entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<IEnumerable<Outfit>> GetBySceneAsync(OutfitScene scene) => Task.FromResult<IEnumerable<Outfit>>([]);
        public Task<IEnumerable<Outfit>> GetBySeasonAsync(Season season) => Task.FromResult<IEnumerable<Outfit>>([]);
        public Task<IEnumerable<Outfit>> GetRecentlyWornAsync(int count) => Task.FromResult<IEnumerable<Outfit>>([]);
        public Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId) => Task.FromResult<IEnumerable<Outfit>>([]);
        public Task DeleteEmptyOutfitsAsync() => Task.CompletedTask;
        public Task<List<OutfitUpdateResult>> DeleteInvalidOutfitsAsync(Guid excludedClothingId) => Task.FromResult(new List<OutfitUpdateResult>());
    }

    private sealed class StubOutfitWornRecordRepository : IOutfitWornRecordRepository
    {
        public Task<IEnumerable<OutfitWornRecord>> GetAllAsync() => Task.FromResult<IEnumerable<OutfitWornRecord>>([]);
        public Task<OutfitWornRecord?> GetByIdAsync(Guid id) => Task.FromResult<OutfitWornRecord?>(null);
        public Task AddAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task UpdateAsync(OutfitWornRecord entity) => Task.CompletedTask;
        public Task DeleteAsync(Guid id) => Task.CompletedTask;
        public Task<int> DeleteAllAsync() => Task.FromResult(0);
        public Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end) => Task.FromResult<IEnumerable<OutfitWornRecord>>([]);
        public Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId) => Task.FromResult<IEnumerable<OutfitWornRecord>>([]);
        public Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count) => Task.FromResult<IEnumerable<OutfitWornRecord>>([]);
        public Task<bool> IsImageReferencedBySnapshotAsync(string imagePath) => Task.FromResult(false);
    }
}
