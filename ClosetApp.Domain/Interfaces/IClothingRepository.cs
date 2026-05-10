using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Interfaces;

public interface IClothingRepository : IRepository<global::ClosetApp.Domain.Entities.Clothing>
{
    Task<IEnumerable<global::ClosetApp.Domain.Entities.Clothing>> GetByTypeAsync(ClothingType type);
}
