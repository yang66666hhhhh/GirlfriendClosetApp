using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.Domain.Interfaces;

public interface IClothingRepository : IRepository<Clothing>
{
    Task<IEnumerable<Clothing>> GetByTypeAsync(ClothingType type);
}
