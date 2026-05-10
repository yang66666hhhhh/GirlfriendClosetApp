using ClosetApp.Domain.Entities;

namespace ClosetApp.Domain.Interfaces;

public interface IOutfitWornRecordRepository : IRepository<OutfitWornRecord>
{
    Task<IEnumerable<OutfitWornRecord>> GetByOutfitIdAsync(Guid outfitId);
}
