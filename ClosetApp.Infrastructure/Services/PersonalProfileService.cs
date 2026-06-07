using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Infrastructure.Services;

public sealed class PersonalProfileService : IPersonalProfileService
{
    private readonly IPersonalProfileRepository _repository;
    private readonly IAiAssetStorageService _assetStorageService;
    private readonly ILocalUserRepository? _localUserRepository;
    private readonly ICurrentUserContext? _currentUserContext;

    public PersonalProfileService(
        IPersonalProfileRepository repository,
        IAiAssetStorageService assetStorageService,
        ILocalUserRepository? localUserRepository = null,
        ICurrentUserContext? currentUserContext = null)
    {
        _repository = repository;
        _assetStorageService = assetStorageService;
        _localUserRepository = localUserRepository;
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

        if (request.RemoveAvatarPhoto)
        {
            await _assetStorageService.TryDeleteProfileReferenceImageAsync(profile.AvatarPhotoPath);
            profile.AvatarPhotoPath = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.AvatarSourcePath))
        {
            var storedFileName = await _assetStorageService.SaveProfileReferenceImageAsync(request.AvatarSourcePath, "avatar");
            profile.AvatarPhotoPath = storedFileName;
        }

        if (request.RemoveFullBodyPhoto)
        {
            await _assetStorageService.TryDeleteProfileReferenceImageAsync(profile.FullBodyPhotoPath);
            profile.FullBodyPhotoPath = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.FullBodySourcePath))
        {
            var storedFileName = await _assetStorageService.SaveProfileReferenceImageAsync(request.FullBodySourcePath, "full-body");
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

        await SyncCurrentUserAvatarAsync(profile.AvatarPhotoPath);
        return profile.ToDto();
    }

    private async Task SyncCurrentUserAvatarAsync(string? avatarPhotoPath)
    {
        if (_localUserRepository == null || _currentUserContext == null)
            return;

        var userId = await _currentUserContext.GetRequiredCurrentUserIdAsync();
        var user = await _localUserRepository.GetActiveByIdAsync(userId);
        if (user == null)
            return;

        user.AvatarPhotoPath = avatarPhotoPath;
        user.UpdatedAt = DateTime.Now;
        await _localUserRepository.UpdateAsync(user);
    }
}
