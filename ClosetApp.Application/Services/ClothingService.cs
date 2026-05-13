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

    public async Task UpdateClothingAsync(Clothing clothing)
    {
        await _repository.UpdateAsync(clothing);
    }

    public async Task DeleteClothingAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        await _outfitRepository.DeleteEmptyOutfitsAsync();
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
