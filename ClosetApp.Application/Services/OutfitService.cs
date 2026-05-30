using System.Text.Json;
using ClosetApp.Application.DTOs;
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
        outfit.Name = NormalizeName(outfit.Name);
        outfit.OriginalClothingCount = outfit.OutfitClothes.Count;
        await _repository.AddAsync(outfit);
        return outfit;
    }

    public async Task UpdateOutfitAsync(Outfit outfit)
    {
        outfit.OriginalClothingCount = outfit.OutfitClothes.Count;
        await _repository.UpdateAsync(outfit);
    }

    public async Task DeleteOutfitAsync(Guid id)
    {
        // 获取搭配及其衣服信息（不使用 AsNoTracking，以便后续删除）
        var outfit = await _repository.GetByIdForUpdateAsync(id);
        if (outfit == null) return;

        // 保存当前搭配的所有衣服信息
        var allClothingDetails = outfit.OutfitClothes
            .Where(oc => oc.Clothing != null)
            .Select(oc => new ClothingSnapshotDto
            {
                Id = oc.ClothingId,
                Name = oc.Clothing!.Name,
                ImagePath = oc.Clothing.ImagePath,
                Color = oc.Clothing.Color,
                Type = oc.Clothing.Type.ToString(),
                GarmentType = oc.Clothing.GarmentType?.ToString()
            })
            .ToList();
        var allClothingDetailsJson = JsonSerializer.Serialize(allClothingDetails);

        // 更新相关穿着记录的快照
        var wornRecords = await _wornRecordRepository.GetByOutfitIdAsync(id);
        foreach (var record in wornRecords)
        {
            if (ShouldRefreshSnapshot(record, allClothingDetails.Count))
            {
                record.OutfitNameSnapshot = outfit.Name;
                record.ClothingCountSnapshot = outfit.OutfitClothes.Count;
                record.ClothingDetailsSnapshot = allClothingDetailsJson;
                record.IsSnapshotComplete = true;
            }
            record.OutfitId = null;
            await _wornRecordRepository.UpdateAsync(record);
        }

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

        var clothingIds = outfit.OutfitClothes
            .Select(oc => oc.ClothingId)
            .ToList();
        var clothingIdsJson = JsonSerializer.Serialize(clothingIds);

        var clothingDetails = outfit.OutfitClothes
            .Where(oc => oc.Clothing != null)
            .Select(oc => new ClothingSnapshotDto
            {
                Id = oc.ClothingId,
                Name = oc.Clothing!.Name,
                ImagePath = oc.Clothing.ImagePath,
                Color = oc.Clothing.Color,
                Type = oc.Clothing.Type.ToString(),
                GarmentType = oc.Clothing.GarmentType?.ToString()
            })
            .ToList();
        var clothingDetailsJson = JsonSerializer.Serialize(clothingDetails);

        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1).AddTicks(-1);
        var existingRecords = await _wornRecordRepository.GetByDateRangeAsync(dayStart, dayEnd);
        var duplicate = existingRecords.FirstOrDefault(r => r.OutfitId == outfitId);

        if (duplicate != null)
        {
            duplicate.WornDate = date;
            duplicate.OutfitNameSnapshot = outfit.Name;
            duplicate.OutfitClothingIdsSnapshot = clothingIdsJson;
            duplicate.ClothingCountSnapshot = clothingIds.Count;
            duplicate.ClothingDetailsSnapshot = clothingDetailsJson;
            duplicate.IsSnapshotComplete = true;
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
            OutfitClothingIdsSnapshot = clothingIdsJson,
            ClothingCountSnapshot = clothingIds.Count,
            ClothingDetailsSnapshot = clothingDetailsJson,
            IsSnapshotComplete = true,
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

    public async Task<WornRecordImageHealthDto> AnalyzeWornRecordImageHealthAsync()
    {
        var records = (await _wornRecordRepository.GetAllAsync()).ToList();
        var snapshotClothingCount = 0;
        var missingImageCount = 0;
        var recordsWithMissingImages = 0;

        foreach (var record in records)
        {
            var missingInRecord = 0;
            foreach (var clothing in DeserializeSnapshotClothes(record.ClothingDetailsSnapshot))
            {
                snapshotClothingCount++;
                if (IsMissingImage(clothing.ImagePath))
                {
                    missingImageCount++;
                    missingInRecord++;
                }
            }

            if (missingInRecord > 0)
                recordsWithMissingImages++;
        }

        return new WornRecordImageHealthDto(
            records.Count,
            snapshotClothingCount,
            missingImageCount,
            recordsWithMissingImages);
    }

    public async Task RepairWornRecordSnapshotImageAsync(Guid recordId, Guid clothingId, string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            throw new ArgumentException("图片路径不能为空。", nameof(imagePath));

        var record = await _wornRecordRepository.GetByIdAsync(recordId)
            ?? throw new InvalidOperationException("穿着记录不存在。");
        var snapshotClothes = DeserializeSnapshotClothes(record.ClothingDetailsSnapshot);
        var target = snapshotClothes.FirstOrDefault(clothing => clothing.Id == clothingId)
            ?? throw new InvalidOperationException("穿着记录里找不到这件单品。");

        target.ImagePath = imagePath;
        record.ClothingDetailsSnapshot = JsonSerializer.Serialize(snapshotClothes);
        record.IsSnapshotComplete = snapshotClothes.Count > 0;
        await _wornRecordRepository.UpdateAsync(record);
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

    private static bool ShouldRefreshSnapshot(OutfitWornRecord record, int currentSnapshotItemCount)
    {
        return !record.IsSnapshotComplete ||
            string.IsNullOrWhiteSpace(record.ClothingDetailsSnapshot) ||
            record.ClothingCountSnapshot < currentSnapshotItemCount;
    }

    private static List<ClothingSnapshotDto> DeserializeSnapshotClothes(string? snapshotJson)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<ClothingSnapshotDto>>(snapshotJson) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static bool IsMissingImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
            return true;

        return !File.Exists(imagePath) &&
            !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, imagePath)) &&
            !File.Exists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClosetApp",
                "images",
                "originals",
                imagePath)) &&
            !File.Exists(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ClosetApp",
                "images",
                "display",
                imagePath)) &&
            !File.Exists(BuildThumbnailPath(imagePath));
    }

    private static string BuildThumbnailPath(string imagePath)
    {
        var thumbnailFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ClosetApp",
            "images",
            "thumbnails");
        var name = Path.GetFileNameWithoutExtension(imagePath);
        var ext = Path.GetExtension(imagePath);
        return Path.Combine(thumbnailFolder, $"{name}_thumb{ext}");
    }
}
