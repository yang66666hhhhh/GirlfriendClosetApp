using ClosetApp.Domain.Entities;

namespace ClosetApp.Domain.Interfaces;

public interface IOutfitWornRecordRepository : IRepository<OutfitWornRecord>
{
    Task<IEnumerable<OutfitWornRecord>> GetByDateRangeAsync(DateTime start, DateTime end);
    Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId);
    Task<IEnumerable<OutfitWornRecord>> GetRecentAsync(int count);
    Task<bool> IsImageReferencedBySnapshotAsync(string imagePath);
}
