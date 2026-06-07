using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Entities;

public class LocalUser : BaseEntity
{
    public string AccountName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarPhotoPath { get; set; }
    public LocalUserRole Role { get; set; } = LocalUserRole.Member;
    public bool IsActive { get; set; } = true;
    public string? LinkedAccountId { get; set; }
    public string? PasswordHash { get; set; }
    public string? PasswordSalt { get; set; }
    public int PasswordIterations { get; set; }
    public string? PinHash { get; set; }
    public string? PinSalt { get; set; }
    public int PinIterations { get; set; }
    public DateTime? CredentialUpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    public bool HasPasswordCredential => !string.IsNullOrWhiteSpace(PasswordHash) && !string.IsNullOrWhiteSpace(PasswordSalt);
    public bool HasPinCredential => !string.IsNullOrWhiteSpace(PinHash) && !string.IsNullOrWhiteSpace(PinSalt);

    public ICollection<Clothing> Clothes { get; set; } = new List<Clothing>();
    public ICollection<Outfit> Outfits { get; set; } = new List<Outfit>();
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<OutfitWornRecord> WornRecords { get; set; } = new List<OutfitWornRecord>();
    public ICollection<PersonalProfile> PersonalProfiles { get; set; } = new List<PersonalProfile>();
    public ICollection<OutfitGeneratedImage> OutfitGeneratedImages { get; set; } = new List<OutfitGeneratedImage>();
}
