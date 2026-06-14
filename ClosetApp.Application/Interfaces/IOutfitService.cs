using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Application.DTOs;

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
    Task<WornRecordImageHealthDto> AnalyzeWornRecordImageHealthAsync();
    Task RepairWornRecordSnapshotImageAsync(Guid recordId, Guid clothingId, string imagePath);
    Task DeleteWornRecordAsync(Guid recordId);
    Task<int> ClearWornHistoryAsync();
    Task<bool> ToggleFavoriteAsync(Guid outfitId);
    Task<IReadOnlyList<OutfitGeneratedImage>> GetGeneratedImagesAsync(Guid outfitId);
}
