using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class PersonalProfileService : IPersonalProfileService
{
    private readonly IPersonalProfileRepository _repository;
    private readonly IAiAssetStorageService _assetStorageService;
    private readonly ICurrentUserContext? _currentUserContext;

    public PersonalProfileService(
        IPersonalProfileRepository repository,
        IAiAssetStorageService assetStorageService,
        ICurrentUserContext? currentUserContext = null)
    {
        _repository = repository;
        _assetStorageService = assetStorageService;
        _currentUserContext = currentUserContext;
    }

    public async Task<PersonalProfileDto?> GetCurrentAsync()
    {
        var profile = await _repository.GetCurrentAsync();
        return profile?.ToDto();
    }

    public async Task<PersonalProfileDto> SaveAsync(SavePersonalProfileRequest request)
    {
        var profile = await _repository.GetCurrentAsync() ?? new PersonalProfile();
        var currentUserId = _currentUserContext == null
            ? (Guid?)null
            : await _currentUserContext.GetRequiredCurrentUserIdAsync();

        if (request.RemoveAvatarPhoto)
        {
            await _assetStorageService.TryDeleteProfileReferenceImageAsync(profile.AvatarPhotoPath, currentUserId);
            profile.AvatarPhotoPath = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.AvatarSourcePath))
        {
            var storedFileName = await _assetStorageService.SaveProfileReferenceImageAsync(
                request.AvatarSourcePath,
                await BuildProfileSlotNameAsync("profile-upper-body"),
                currentUserId);
            profile.AvatarPhotoPath = storedFileName;
        }

        if (request.RemoveFullBodyPhoto)
        {
            await _assetStorageService.TryDeleteProfileReferenceImageAsync(profile.FullBodyPhotoPath, currentUserId);
            profile.FullBodyPhotoPath = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.FullBodySourcePath))
        {
            var storedFileName = await _assetStorageService.SaveProfileReferenceImageAsync(
                request.FullBodySourcePath,
                await BuildProfileSlotNameAsync("full-body"),
                currentUserId);
            profile.FullBodyPhotoPath = storedFileName;
        }

        profile.DisplayName = request.DisplayName.Trim();
        profile.HeightCm = request.HeightCm;
        profile.BodyShape = request.BodyShape.Trim();
        profile.SkinTone = request.SkinTone.Trim();
        profile.HairLength = request.HairLength.Trim();
        profile.HairColor = request.HairColor.Trim();
        profile.FaceFeaturesSummary = request.FaceFeaturesSummary.Trim();
        profile.StyleKeywords = request.StyleKeywords.Trim();
        profile.AvoidKeywords = request.AvoidKeywords.Trim();
        profile.CloudUploadConsentAcceptedAt = request.AcceptCloudUploadConsent
            ? profile.CloudUploadConsentAcceptedAt ?? DateTime.Now
            : null;
        profile.UpdatedAt = DateTime.Now;

        if (profile.Id == Guid.Empty)
            profile.Id = Guid.NewGuid();

        var existing = await _repository.GetByIdAsync(profile.Id);
        if (existing == null)
            await _repository.AddAsync(profile);
        else
            await _repository.UpdateAsync(profile);

        return profile.ToDto();
    }

    private async Task<string> BuildProfileSlotNameAsync(string slotSuffix)
    {
        if (_currentUserContext == null)
            return slotSuffix;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        return $"user-{userId:N}-{slotSuffix}";
    }
}
