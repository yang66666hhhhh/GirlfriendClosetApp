using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Application.UseCases.Outfits;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;
using System.Net.Http;
using Xunit;

namespace ClosetApp.Tests;

public class GenerateOutfitEffectImageTests
{
    [Fact]
    public async Task ExecuteAsync_WithMatchingSavedImage_ReusesExistingResultWithoutCallingProvider()
    {
        var outfit = CreateOutfit();
        var profileDto = new PersonalProfileDto(
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
            DateTime.Now);
        var request = new GenerateOutfitEffectImageRequest(
            outfit.Id,
            "通勤",
            "站姿正面",
            "城市街景",
            "全身",
            "松弛");
        var existingImage = new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            OutfitId = outfit.Id,
            ProviderKind = "OpenAI-Compatible",
            Model = "gpt-image-1",
            PromptSnapshot = "prompt",
            ProfileSnapshotJson = BuildProfileSnapshot(profileDto),
            OutfitSnapshotJson = BuildOutfitSnapshot(outfit),
            OptionSnapshotJson = System.Text.Json.JsonSerializer.Serialize(request),
            ResultImagePath = "saved.png",
            IsPrimary = true,
            Status = "Succeeded",
            CreatedAt = DateTime.Now.AddMinutes(-5)
        };

        var generationService = new FakeAiImageGenerationService();
        var repository = new FakeOutfitGeneratedImageRepository(existingImage);
        var useCase = new GenerateOutfitEffectImage(
            new FakeOutfitService(outfit),
            new FakePersonalProfileService(profileDto),
            new FakeAiGenerationPreferencesService(),
            generationService,
            repository,
            new FakeAiAssetStorageService(),
            new GetAiGenerationReadiness(
                new FakePersonalProfileService(profileDto),
                new FakeAiGenerationPreferencesService(),
                new FakeOutfitService(outfit)));

        var result = await useCase.ExecuteAsync(request);

        Assert.True(result.WasReused);
        Assert.Equal(existingImage.Id, result.Id);
        Assert.Equal(0, generationService.CallCount);
        Assert.Single(repository.Images);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProviderFails_PersistsFailedAttempt()
    {
        var outfit = CreateOutfit();
        var profileDto = new PersonalProfileDto(
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
            DateTime.Now);
        var request = new GenerateOutfitEffectImageRequest(
            outfit.Id,
            "通勤",
            "站姿正面",
            "城市街景",
            "全身",
            "松弛");
        var repository = new FakeOutfitGeneratedImageRepository();
        var useCase = new GenerateOutfitEffectImage(
            new FakeOutfitService(outfit),
            new FakePersonalProfileService(profileDto),
            new FakeAiGenerationPreferencesService(),
            new ThrowingAiImageGenerationService("status_code=524"),
            repository,
            new FakeAiAssetStorageService(),
            new GetAiGenerationReadiness(
                new FakePersonalProfileService(profileDto),
                new FakeAiGenerationPreferencesService(),
                new FakeOutfitService(outfit)));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => useCase.ExecuteAsync(request));

        Assert.Contains("524", exception.Message);
        var failedAttempt = Assert.Single(repository.Images);
        Assert.Equal("Failed", failedAttempt.Status);
        Assert.Equal("status_code=524", failedAttempt.FailureReason);
        Assert.Null(failedAttempt.ResultImagePath);
    }

    private static Outfit CreateOutfit()
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = "测试搭配",
            Scene = OutfitScene.Work,
            Season = Season.Autumn
        };

        for (var i = 0; i < 2; i++)
        {
            var clothing = new Clothing
            {
                Id = Guid.NewGuid(),
                Name = $"单品{i + 1}",
                Color = i == 0 ? "粉色" : "白色",
                Brand = "测试品牌",
                Type = i == 0 ? ClothingType.Top : ClothingType.Bottom,
                GarmentType = i == 0 ? Domain.Clothing.GarmentType.Blouse : Domain.Clothing.GarmentType.Jeans
            };
            outfit.OutfitClothes.Add(new OutfitClothing
            {
                OutfitId = outfit.Id,
                ClothingId = clothing.Id,
                Clothing = clothing
            });
        }

        return outfit;
    }

    private static string BuildProfileSnapshot(PersonalProfileDto profile)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            profile.DisplayName,
            profile.HeightCm,
            profile.BodyShape,
            profile.SkinTone,
            profile.HairLength,
            profile.HairColor,
            profile.FaceFeaturesSummary,
            profile.StyleKeywords,
            profile.AvoidKeywords,
            profile.AvatarPhotoPath,
            profile.FullBodyPhotoPath
        });
    }

    private static string BuildOutfitSnapshot(Outfit outfit)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            outfit.Id,
            outfit.Name,
            outfit.Scene,
            outfit.Season,
            outfit.Rating,
            Clothes = outfit.OutfitClothes
                .Where(link => link.Clothing != null)
                .Select(link => new
                {
                    link.ClothingId,
                    link.Clothing!.Name,
                    link.Clothing.Color,
                    link.Clothing.Brand,
                    link.Clothing.Type,
                    link.Clothing.GarmentType,
                    Tags = link.Clothing.ClothingTags.Select(tag => tag.Tag?.Name).Where(name => !string.IsNullOrWhiteSpace(name)).ToList()
                })
                .ToList()
        });
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
        public Task<AiGenerationPreferences> GetAsync() => Task.FromResult(new AiGenerationPreferences(
            "https://api.openai.com/v1",
            "gpt-image-1",
            60,
            HasEncryptedApiKey: true));

        public Task SaveAsync(SaveAiGenerationPreferencesRequest request) => throw new NotImplementedException();
        public Task<string?> GetApiKeyAsync() => Task.FromResult<string?>("test-key");
        public Task MarkConnectionCheckedAsync(DateTime checkedAt) => Task.CompletedTask;
    }

    private sealed class FakeAiImageGenerationService : IAiImageGenerationService
    {
        public int CallCount { get; private set; }

        public Task TestConnectionAsync() => Task.CompletedTask;

        public Task<AiImageGenerationResponse> GenerateOutfitEffectImageAsync(
            PersonalProfile profile,
            Outfit outfit,
            GenerateOutfitEffectImageRequest request,
            AiGenerationPreferences preferences,
            string apiKey)
        {
            CallCount++;
            throw new InvalidOperationException("命中已保存结果时不应再调用生成服务。");
        }
    }

    private sealed class ThrowingAiImageGenerationService : IAiImageGenerationService
    {
        private readonly string _message;

        public ThrowingAiImageGenerationService(string message)
        {
            _message = message;
        }

        public Task TestConnectionAsync() => Task.CompletedTask;

        public Task<AiImageGenerationResponse> GenerateOutfitEffectImageAsync(
            PersonalProfile profile,
            Outfit outfit,
            GenerateOutfitEffectImageRequest request,
            AiGenerationPreferences preferences,
            string apiKey)
        {
            throw new HttpRequestException(_message);
        }
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
            return Task.FromResult<IReadOnlyList<OutfitGeneratedImage>>(Images.Where(image => image.OutfitId == outfitId).ToList());
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
        public Task<string> SaveProfileReferenceImageAsync(string sourcePath, string slotName, Guid? userId = null) => throw new NotImplementedException();
        public Task<string> SaveGeneratedImageAsync(byte[] bytes, string mimeType) => throw new NotImplementedException();
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
