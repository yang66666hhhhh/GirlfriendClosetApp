using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Application.Interfaces;

public interface IOutfitService
{
    Task<IEnumerable<Outfit>> GetAllOutfitsAsync();
    Task<Outfit?> GetOutfitByIdAsync(Guid id);
    Task<Outfit> AddOutfitAsync(Outfit outfit);
    Task UpdateOutfitAsync(Outfit outfit);
    Task DeleteOutfitAsync(Guid id);
    Task<IEnumerable<Outfit>> GetOutfitsBySceneAsync(OutfitScene scene);
    Task<IEnumerable<Outfit>> GetRecentlyWornOutfitsAsync(int count);
    Task<IEnumerable<OutfitWornRecord>> GetRecentWornRecordsAsync(int count);
    Task<IEnumerable<OutfitWornRecord>> GetWornRecordsAsync(DateTime start, DateTime end);
    Task RecordWornDateAsync(Guid outfitId, DateTime date);
    Task DeleteWornRecordAsync(Guid recordId);
}
