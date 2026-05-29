using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class ClothingService : IClothingService
{
    private readonly IClothingRepository _repository;
    private readonly IOutfitRepository _outfitRepository;

    public ClothingService(IClothingRepository repository, IOutfitRepository outfitRepository)
    {
        _repository = repository;
        _outfitRepository = outfitRepository;
    }

    public async Task<IEnumerable<Clothing>> GetAllClothesAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Clothing?> GetClothingByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Clothing> AddClothingAsync(Clothing clothing)
    {
        await _repository.AddAsync(clothing);
        return clothing;
    }

    public async Task AddClothesAsync(IEnumerable<Clothing> clothes)
    {
        foreach (var clothing in clothes)
            await _repository.AddAsync(clothing);
    }

    public async Task UpdateClothingAsync(Clothing clothing)
    {
        await _repository.UpdateAsync(clothing);
    }

    public async Task<ClothingDeleteResult> DeleteClothingAsync(Guid id)
    {
        var clothing = await _repository.GetByIdAsync(id);
        var clothingName = clothing?.Name ?? "未知衣物";

        var outfitResults = await _outfitRepository.DeleteInvalidOutfitsAsync(id);
        
        await _repository.DeleteAsync(id);

        return new ClothingDeleteResult
        {
            Success = true,
            DeletedClothingName = clothingName,
            UpdatedOutfits = outfitResults
        };
    }

    public async Task<IEnumerable<Outfit>> GetOutfitsByClothingIdAsync(Guid clothingId)
    {
        return await _outfitRepository.GetOutfitsByClothingIdAsync(clothingId);
    }

    public async Task<IEnumerable<Clothing>> GetClothesByTypeAsync(ClothingType type)
    {
        return await _repository.GetByTypeAsync(type);
    }

    public async Task<IEnumerable<Clothing>> SearchClothesAsync(string keyword)
    {
        var all = await _repository.GetAllAsync();
        return all.Where(c => c.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
