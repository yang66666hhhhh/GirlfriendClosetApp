using ClosetApp.Application.DTOs;

namespace ClosetApp.Application.Interfaces;

public interface IPersonalProfileService
{
    Task<PersonalProfileDto?> GetCurrentAsync();
    Task<PersonalProfileDto> SaveAsync(SavePersonalProfileRequest request);
}
