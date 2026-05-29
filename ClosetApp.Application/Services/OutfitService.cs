using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.Domain.Interfaces;

namespace ClosetApp.Application.Services;

public class OutfitService : IOutfitService
{
    private const string DefaultOutfitName = "未命名";

    private readonly IOutfitRepository _repository;
    private readonly IOutfitWornRecordRepository _wornRecordRepository;
    private readonly IFavoriteRepository _favoriteRepository;

    public OutfitService(
        IOutfitRepository repository,
        IOutfitWornRecordRepository wornRecordRepository,
        IFavoriteRepository favoriteRepository)
    {
        _repository = repository;
        _wornRecordRepository = wornRecordRepository;
        _favoriteRepository = favoriteRepository;
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
        // 创建搭配时允许名称留空，统一补一个稳定的默认名。
        outfit.Name = NormalizeName(outfit.Name);
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

    public async Task<IEnumerable<OutfitWornRecord>> GetRecentWornRecordsAsync(int count)
    {
        return await _wornRecordRepository.GetRecentAsync(count);
    }

    public async Task<IEnumerable<OutfitWornRecord>> GetWornRecordsAsync(DateTime start, DateTime end)
    {
        return await _wornRecordRepository.GetByDateRangeAsync(start, end);
    }

    public async Task RecordWornDateAsync(Guid outfitId, DateTime date)
    {
        var outfit = await _repository.GetByIdAsync(outfitId);
        if (outfit == null) return;

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);
        var existingRecords = await _wornRecordRepository.GetByDateRangeAsync(dayStart, dayEnd);
        var duplicate = existingRecords.FirstOrDefault(r => r.OutfitId == outfitId);

        if (duplicate != null)
        {
            duplicate.WornDate = date;
            duplicate.OutfitNameSnapshot = outfit.Name;
            await _wornRecordRepository.UpdateAsync(duplicate);
            outfit.WornDate = date;
            await _repository.UpdateAsync(outfit);
            return;
        }

        outfit.WornDate = date;
        outfit.WearCount++;
        await _wornRecordRepository.AddAsync(new OutfitWornRecord
        {
            OutfitId = outfitId,
            OutfitNameSnapshot = outfit.Name,
            WornDate = date
        });
        await _repository.UpdateAsync(outfit);
    }

    public async Task DeleteWornRecordAsync(Guid recordId)
    {
        var record = await _wornRecordRepository.GetByIdAsync(recordId);
        if (record == null)
            return;

        if (record.OutfitId.HasValue)
        {
            var outfit = await _repository.GetByIdAsync(record.OutfitId.Value);
            await _wornRecordRepository.DeleteAsync(recordId);

            if (outfit == null)
                return;

            outfit.WearCount = Math.Max(0, outfit.WearCount - 1);
            var remainingRecords = await _wornRecordRepository.GetByOutfitIdAsync(outfit.Id);
            outfit.WornDate = remainingRecords.FirstOrDefault()?.WornDate;
            await _repository.UpdateAsync(outfit);
        }
        else
        {
            await _wornRecordRepository.DeleteAsync(recordId);
        }
    }

    public async Task<bool> ToggleFavoriteAsync(Guid outfitId)
    {
        var existing = (await _favoriteRepository.GetByOutfitIdAsync(outfitId)).FirstOrDefault();
        if (existing != null)
        {
            await _favoriteRepository.DeleteAsync(existing.Id);
            return false;
        }

        await _favoriteRepository.AddAsync(new Favorite { OutfitId = outfitId });
        return true;
    }

    private static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? DefaultOutfitName : trimmed;
    }
}
