using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class OutfitService : IOutfitService
{
    private readonly IOutfitRepository _repository;

    public OutfitService(IOutfitRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Outfit>> GetAllOutfitsAsync()
    {
        return await _repository.GetAllAsync();
    }

    public async Task<Outfit?> GetOutfitByIdAsync(Guid id)
    {
        return await _repository.GetByIdAsync(id);
    }

    public async Task<Outfit> AddOutfitAsync(Outfit outfit)
    {
        await _repository.AddAsync(outfit);
        return outfit;
    }

    public async Task UpdateOutfitAsync(Outfit outfit)
    {
        await _repository.UpdateAsync(outfit);
    }

    public async Task DeleteOutfitAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
    }

    public async Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene)
    {
        return await _repository.GetBySceneAsync(scene);
    }

    public async Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count)
    {
        return await _repository.GetRecentlyWornAsync(count);
    }

    public async Task RecordWornDateAsync(Guid outfitId, DateTime date)
    {
        var outfit = await _repository.GetByIdAsync(outfitId);
        if (outfit != null)
        {
            outfit.WornDate = date;
            outfit.WearCount++;
            await _repository.UpdateAsync(outfit);
        }
    }
}