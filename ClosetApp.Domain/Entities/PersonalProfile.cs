namespace ClosetApp.Domain.Entities;

public class PersonalProfile : BaseEntity
{
    public Guid? LocalUserId { get; set; }
    public LocalUser? LocalUser { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public int? HeightCm { get; set; }
    public string BodyShape { get; set; } = string.Empty;
    public string SkinTone { get; set; } = string.Empty;
    public string HairLength { get; set; } = string.Empty;
    public string HairColor { get; set; } = string.Empty;
    public string FaceFeaturesSummary { get; set; } = string.Empty;
    public string StyleKeywords { get; set; } = string.Empty;
    public string AvoidKeywords { get; set; } = string.Empty;
    public string? AvatarPhotoPath { get; set; }
    public string? FullBodyPhotoPath { get; set; }
    public DateTime? CloudUploadConsentAcceptedAt { get; set; }
}
