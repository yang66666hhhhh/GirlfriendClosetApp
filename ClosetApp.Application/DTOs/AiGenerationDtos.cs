using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.DTOs;

public sealed record PersonalProfileDto(
    Guid Id,
    string DisplayName,
    int? HeightCm,
    string BodyShape,
    string SkinTone,
    string HairLength,
    string HairColor,
    string FaceFeaturesSummary,
    string StyleKeywords,
    string AvoidKeywords,
    string? AvatarPhotoPath,
    string? FullBodyPhotoPath,
    DateTime? CloudUploadConsentAcceptedAt)
{
    public bool HasAvatarPhoto => !string.IsNullOrWhiteSpace(AvatarPhotoPath);

    public bool HasFullBodyPhoto => !string.IsNullOrWhiteSpace(FullBodyPhotoPath);

    public bool HasReferencePhotos =>
        HasAvatarPhoto && HasFullBodyPhoto;

    public bool HasMinimumReferencePhotos => HasAvatarPhoto;

    public bool HasConsent => CloudUploadConsentAcceptedAt.HasValue;
}

public sealed record SavePersonalProfileRequest(
    string DisplayName,
    int? HeightCm,
    string BodyShape,
    string SkinTone,
    string HairLength,
    string HairColor,
    string FaceFeaturesSummary,
    string StyleKeywords,
    string AvoidKeywords,
    string? AvatarSourcePath,
    string? FullBodySourcePath,
    bool AcceptCloudUploadConsent,
    bool RemoveAvatarPhoto = false,
    bool RemoveFullBodyPhoto = false);

public sealed record AiGenerationPreferences(
    string BaseUrl,
    string Model,
    int TimeoutSeconds,
    DateTime? LastConnectionCheckAt = null,
    bool HasEncryptedApiKey = false);

public sealed record SaveAiGenerationPreferencesRequest(
    string BaseUrl,
    string Model,
    int TimeoutSeconds,
    string? ApiKey,
    bool ClearApiKey = false);

public sealed record OutfitGeneratedImageDto(
    Guid Id,
    Guid OutfitId,
    string ProviderKind,
    string Model,
    string PromptSnapshot,
    string ProfileSnapshotJson,
    string OutfitSnapshotJson,
    string OptionSnapshotJson,
    string? ResultImagePath,
    bool IsPrimary,
    string Status,
    string? FailureReason,
    DateTime CreatedAt,
    bool WasReused = false);

public sealed record GenerateOutfitEffectImageRequest(
    Guid OutfitId,
    string Scene,
    string Pose,
    string BackgroundStyle,
    string Framing,
    string Mood);

public sealed record SaveUploadedOutfitGeneratedImageRequest(
    Guid OutfitId,
    string SourcePath,
    string? DisplayName = null,
    string? Note = null);

public sealed record AiGenerationReadinessResult(
    bool CanGenerate,
    IReadOnlyList<string> BlockingReasons,
    PersonalProfileDto? Profile,
    AiGenerationPreferences Preferences)
{
    public string Summary =>
        CanGenerate
            ? "当前资料已经满足生成条件。"
            : string.Join(" ", BlockingReasons);
}

public sealed record AiImageGenerationResponse(
    string ProviderKind,
    string Model,
    string Prompt,
    byte[] ImageBytes,
    string MimeType,
    string ProfileSnapshotJson,
    string OutfitSnapshotJson,
    string OptionSnapshotJson);

public sealed record OutfitGenerationOption(
    string Label,
    string Value);

public sealed record OutfitGenerationOptions(
    IReadOnlyList<OutfitGenerationOption> SceneOptions,
    IReadOnlyList<OutfitGenerationOption> PoseOptions,
    IReadOnlyList<OutfitGenerationOption> BackgroundStyleOptions,
    IReadOnlyList<OutfitGenerationOption> FramingOptions,
    IReadOnlyList<OutfitGenerationOption> MoodOptions);

public static class AiGenerationMappings
{
    public static PersonalProfileDto ToDto(this PersonalProfile profile)
    {
        return new PersonalProfileDto(
            profile.Id,
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
            profile.FullBodyPhotoPath,
            profile.CloudUploadConsentAcceptedAt);
    }

    public static OutfitGeneratedImageDto ToDto(this OutfitGeneratedImage image)
    {
        return new OutfitGeneratedImageDto(
            image.Id,
            image.OutfitId,
            image.ProviderKind,
            image.Model,
            image.PromptSnapshot,
            image.ProfileSnapshotJson,
            image.OutfitSnapshotJson,
            image.OptionSnapshotJson,
            image.ResultImagePath,
            image.IsPrimary,
            image.Status,
            image.FailureReason,
            image.CreatedAt,
            false);
    }
}
