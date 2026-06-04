using ClosetApp.Domain.Entities;

namespace ClosetApp.Domain.Interfaces;

public interface IPersonalProfileRepository : IRepository<PersonalProfile>
{
    Task<PersonalProfile?> GetCurrentAsync();
}
