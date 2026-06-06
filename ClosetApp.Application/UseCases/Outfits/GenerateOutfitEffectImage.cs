using System.Text.Json;
using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class GenerateOutfitEffectImage
{
    private readonly IOutfitService _outfitService;
    private readonly IPersonalProfileService _personalProfileService;
    private readonly IAiGenerationPreferencesService _preferencesService;
    private readonly IAiImageGenerationService _generationService;
    private readonly IOutfitGeneratedImageRepository _generatedImageRepository;
    private readonly IAiAssetStorageService _assetStorageService;
    private readonly GetAiGenerationReadiness _readiness;

    public GenerateOutfitEffectImage(
        IOutfitService outfitService,
        IPersonalProfileService personalProfileService,
        IAiGenerationPreferencesService preferencesService,
        IAiImageGenerationService generationService,
        IOutfitGeneratedImageRepository generatedImageRepository,
        IAiAssetStorageService assetStorageService,
        GetAiGenerationReadiness readiness)
    {
        _outfitService = outfitService;
        _personalProfileService = personalProfileService;
        _preferencesService = preferencesService;
        _generationService = generationService;
        _generatedImageRepository = generatedImageRepository;
        _assetStorageService = assetStorageService;
        _readiness = readiness;
    }

    public async Task<OutfitGeneratedImageDto> ExecuteAsync(GenerateOutfitEffectImageRequest request)
    {
        var readiness = await _readiness.ExecuteAsync(request.OutfitId);
        if (!readiness.CanGenerate)
            throw new InvalidOperationException(readiness.Summary);

        var outfit = await _outfitService.GetOutfitByIdAsync(request.OutfitId)
            ?? throw new InvalidOperationException("搭配不存在。");
        var profile = await _personalProfileService.GetCurrentAsync();
        if (profile == null)
            throw new InvalidOperationException("个人档案不存在。");

        var preferences = await _preferencesService.GetAsync();
        var apiKey = await _preferencesService.GetApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("还没有可用的 API Key。");

        var profileEntity = new PersonalProfile
        {
            Id = profile.Id,
            DisplayName = profile.DisplayName,
            HeightCm = profile.HeightCm,
            BodyShape = profile.BodyShape,
            SkinTone = profile.SkinTone,
            HairLength = profile.HairLength,
            HairColor = profile.HairColor,
            FaceFeaturesSummary = profile.FaceFeaturesSummary,
            StyleKeywords = profile.StyleKeywords,
            AvoidKeywords = profile.AvoidKeywords,
            AvatarPhotoPath = profile.AvatarPhotoPath,
            FullBodyPhotoPath = profile.FullBodyPhotoPath,
            CloudUploadConsentAcceptedAt = profile.CloudUploadConsentAcceptedAt
        };

        var optionSnapshot = JsonSerializer.Serialize(request);
        var profileSnapshot = BuildProfileSnapshot(profileEntity);
        var outfitSnapshot = BuildOutfitSnapshot(outfit);
        var existingImages = await _generatedImageRepository.GetByOutfitIdAsync(outfit.Id);
        var reusableImage = existingImages
            .OrderByDescending(image => image.IsPrimary)
            .ThenByDescending(image => image.CreatedAt)
            .FirstOrDefault(image =>
                string.Equals(image.Status, "Succeeded", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(image.ResultImagePath) &&
                string.Equals(image.ProfileSnapshotJson, profileSnapshot, StringComparison.Ordinal) &&
                string.Equals(image.OutfitSnapshotJson, outfitSnapshot, StringComparison.Ordinal) &&
                string.Equals(image.OptionSnapshotJson, optionSnapshot, StringComparison.Ordinal));

        if (reusableImage != null)
            return reusableImage.ToDto() with { WasReused = true };

        var pendingImage = new OutfitGeneratedImage
        {
            OutfitId = outfit.Id,
            ProviderKind = "OpenAI-Compatible",
            Model = preferences.Model,
            PromptSnapshot = string.Empty,
            ProfileSnapshotJson = profileSnapshot,
            OutfitSnapshotJson = outfitSnapshot,
            OptionSnapshotJson = optionSnapshot,
            ResultImagePath = null,
            IsPrimary = false,
            Status = "Pending",
            FailureReason = null
        };
        await _generatedImageRepository.AddAsync(pendingImage);

        string? storedFileName = null;
        try
        {
            var response = await _generationService.GenerateOutfitEffectImageAsync(
                profileEntity,
                outfit,
                request,
                preferences,
                apiKey);

            storedFileName = await _assetStorageService.SaveGeneratedImageAsync(response.ImageBytes, response.MimeType);
            var image = new OutfitGeneratedImage
            {
                OutfitId = outfit.Id,
                ProviderKind = response.ProviderKind,
                Model = response.Model,
                PromptSnapshot = response.Prompt,
                ProfileSnapshotJson = response.ProfileSnapshotJson,
                OutfitSnapshotJson = response.OutfitSnapshotJson,
                OptionSnapshotJson = response.OptionSnapshotJson,
                ResultImagePath = storedFileName,
                IsPrimary = !existingImages.Any(),
                Status = "Succeeded",
                FailureReason = null
            };

            pendingImage.ProviderKind = image.ProviderKind;
            pendingImage.Model = image.Model;
            pendingImage.PromptSnapshot = image.PromptSnapshot;
            pendingImage.ProfileSnapshotJson = image.ProfileSnapshotJson;
            pendingImage.OutfitSnapshotJson = image.OutfitSnapshotJson;
            pendingImage.OptionSnapshotJson = image.OptionSnapshotJson;
            pendingImage.ResultImagePath = image.ResultImagePath;
            pendingImage.IsPrimary = image.IsPrimary;
            pendingImage.Status = image.Status;
            pendingImage.FailureReason = null;

            if (pendingImage.IsPrimary)
                await _generatedImageRepository.ClearPrimaryAsync(outfit.Id);

            await _generatedImageRepository.UpdateAsync(pendingImage);
            return pendingImage.ToDto();
        }
        catch (Exception ex)
        {
            if (!string.IsNullOrWhiteSpace(storedFileName))
                await _assetStorageService.TryDeleteGeneratedImageAsync(storedFileName);

            pendingImage.Status = "Failed";
            pendingImage.FailureReason = ex.Message;
            pendingImage.ResultImagePath = null;
            pendingImage.IsPrimary = false;
            await _generatedImageRepository.UpdateAsync(pendingImage);
            throw;
        }
    }

    private static string BuildProfileSnapshot(PersonalProfile profile)
    {
        return JsonSerializer.Serialize(new
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
        return JsonSerializer.Serialize(new
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
}
