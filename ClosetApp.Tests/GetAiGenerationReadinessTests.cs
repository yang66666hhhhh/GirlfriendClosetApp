using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using Xunit;

namespace ClosetApp.Tests;

public class GetAiGenerationReadinessTests
{
    [Fact]
    public async Task ExecuteAsync_WithCompleteProfileAndConfiguredProvider_ReturnsReady()
    {
        var outfit = CreateOutfit(withValidClothes: true);
        var useCase = new GetAiGenerationReadiness(
            new FakePersonalProfileService(new PersonalProfileDto(
                Guid.NewGuid(),
                "小楠",
                165,
                "匀称",
                "自然白",
                "中长发",
                "深棕",
                "五官柔和",
                "通勤、极简",
                "过度夸张",
                "avatar.png",
                "full-body.png",
                DateTime.Now)),
            new FakeAiGenerationPreferencesService(new AiGenerationPreferences(
                "https://api.openai.com/v1",
                "gpt-image-1",
                60,
                HasEncryptedApiKey: true)),
            new FakeOutfitService(outfit));

        var result = await useCase.ExecuteAsync(outfit.Id);

        Assert.True(result.CanGenerate);
        Assert.Empty(result.BlockingReasons);
    }

    [Fact]
    public async Task ExecuteAsync_WithAvatarOnlyProfileAndConfiguredProvider_ReturnsReady()
    {
        var outfit = CreateOutfit(withValidClothes: true);
        var useCase = new GetAiGenerationReadiness(
            new FakePersonalProfileService(new PersonalProfileDto(
                Guid.NewGuid(),
                "小楠",
                165,
                "匀称",
                "自然白",
                "中长发",
                "深棕",
                "五官柔和",
                "通勤、极简",
                "过度夸张",
                "avatar.png",
                null,
                DateTime.Now)),
            new FakeAiGenerationPreferencesService(new AiGenerationPreferences(
                "https://api.openai.com/v1",
                "gpt-image-1",
                60,
                HasEncryptedApiKey: true)),
            new FakeOutfitService(outfit));

        var result = await useCase.ExecuteAsync(outfit.Id);

        Assert.True(result.CanGenerate);
        Assert.DoesNotContain(result.BlockingReasons, reason => reason.Contains("头像", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WithGptImage2AndNoAvatar_ReturnsReady()
    {
        var outfit = CreateOutfit(withValidClothes: true);
        var useCase = new GetAiGenerationReadiness(
            new FakePersonalProfileService(new PersonalProfileDto(
                Guid.NewGuid(),
                "小楠",
                165,
                "匀称",
                "自然白",
                "中长发",
                "深棕",
                "五官柔和",
                "通勤、极简",
                "过度夸张",
                null,
                null,
                DateTime.Now)),
            new FakeAiGenerationPreferencesService(new AiGenerationPreferences(
                "https://api.sbbbbbbbbb.xyz",
                "gpt-image-2",
                180,
                HasEncryptedApiKey: true)),
            new FakeOutfitService(outfit));

        var result = await useCase.ExecuteAsync(outfit.Id);

        Assert.True(result.CanGenerate);
        Assert.DoesNotContain(result.BlockingReasons, reason => reason.Contains("头像", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingProfileAndProviderConfig_ReturnsBlockingReasons()
    {
        var outfit = CreateOutfit(withValidClothes: false);
        var useCase = new GetAiGenerationReadiness(
            new FakePersonalProfileService(null),
            new FakeAiGenerationPreferencesService(new AiGenerationPreferences("", "", 60, HasEncryptedApiKey: false)),
            new FakeOutfitService(outfit));

        var result = await useCase.ExecuteAsync(outfit.Id);

        Assert.False(result.CanGenerate);
        Assert.Contains(result.BlockingReasons, reason => reason.Contains("个人档案", StringComparison.Ordinal));
        Assert.Contains(result.BlockingReasons, reason => reason.Contains("provider", StringComparison.OrdinalIgnoreCase) || reason.Contains("配置", StringComparison.Ordinal));
        Assert.Contains(result.BlockingReasons, reason => reason.Contains("至少需要 2 件", StringComparison.Ordinal));
    }

    private static Outfit CreateOutfit(bool withValidClothes)
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = "测试搭配",
            Scene = OutfitScene.Work,
            Season = Season.Autumn
        };

        if (withValidClothes)
        {
            for (var i = 0; i < 2; i++)
            {
                var clothing = new Clothing
                {
                    Id = Guid.NewGuid(),
                    Name = $"单品{i + 1}"
                };
                outfit.OutfitClothes.Add(new OutfitClothing
                {
                    OutfitId = outfit.Id,
                    ClothingId = clothing.Id,
                    Clothing = clothing
                });
            }
        }

        return outfit;
    }

    private sealed class FakePersonalProfileService : IPersonalProfileService
    {
        private readonly PersonalProfileDto? _profile;

        public FakePersonalProfileService(PersonalProfileDto? profile)
        {
            _profile = profile;
        }

        public Task<PersonalProfileDto?> GetCurrentAsync() => Task.FromResult(_profile);
        public Task<PersonalProfileDto> SaveAsync(SavePersonalProfileRequest request) => throw new NotImplementedException();
    }

    private sealed class FakeAiGenerationPreferencesService : IAiGenerationPreferencesService
    {
        private readonly AiGenerationPreferences _preferences;

        public FakeAiGenerationPreferencesService(AiGenerationPreferences preferences)
        {
            _preferences = preferences;
        }

        public Task<AiGenerationPreferences> GetAsync() => Task.FromResult(_preferences);
        public Task SaveAsync(SaveAiGenerationPreferencesRequest request) => throw new NotImplementedException();
        public Task<string?> GetApiKeyAsync() => Task.FromResult<string?>("test-key");
        public Task MarkConnectionCheckedAsync(DateTime checkedAt) => Task.CompletedTask;
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
}
