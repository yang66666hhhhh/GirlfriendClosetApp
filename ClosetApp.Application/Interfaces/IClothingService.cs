using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.Interfaces;

public interface IClothingService
{
    Task<IEnumerable<Clothing>> GetAllClothesAsync();
    Task<Clothing?> GetClothingByIdAsync(Guid id);
    Task<Clothing> AddClothingAsync(Clothing clothing);
    Task AddClothesAsync(IEnumerable<Clothing> clothes);
    Task UpdateClothingAsync(Clothing clothing);
    Task DeleteClothingAsync(Guid id);
    Task<IEnumerable<Clothing>> GetClothesByTypeAsync(ClothingType type);
    Task<IEnumerable<Clothing>> SearchClothesAsync(string keyword);
}
