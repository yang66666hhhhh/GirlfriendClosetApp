using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Interfaces;

public interface IClothingService
{
    Task<IEnumerable<Clothing>> GetAllClothesAsync();
    Task<Clothing?> GetClothingByIdAsync(Guid id);
    Task<Clothing> AddClothingAsync(Clothing clothing);
    Task AddClothesAsync(IEnumerable<Clothing> clothes);
    Task UpdateClothingAsync(Clothing clothing);
    Task<ClothingDeleteResult> DeleteClothingAsync(Guid id);
    Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId);
    Task<IEnumerable<Clothing>> GetClothesByTypeAsync(ClothingType type);
    Task<IEnumerable<Clothing>> SearchClothesAsync(string keyword);
}

public class ClothingDeleteResult
{
    public bool Success { get; set; }
    public string DeletedClothingName { get; set; } = string.Empty;
    public bool PreserveDeletedImageForHistory { get; set; }
    public List<OutfitUpdateResult> UpdatedOutfits { get; set; } = new();
}
