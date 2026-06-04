using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.Interfaces;

public interface IAiImageGenerationService
{
    Task TestConnectionAsync();
    Task<AiImageGenerationResponse> GenerateOutfitEffectImageAsync(
        PersonalProfile profile,
        Outfit outfit,
        GenerateOutfitEffectImageRequest request,
        AiGenerationPreferences preferences,
        string apiKey);
}
