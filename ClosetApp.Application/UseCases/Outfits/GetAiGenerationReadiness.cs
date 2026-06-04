using ClosetApp.Application.DTOs;
using ClosetApp.Application.Interfaces;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.UseCases.Outfits;

public sealed class GetAiGenerationReadiness
{
    private readonly IPersonalProfileService _personalProfileService;
    private readonly IAiGenerationPreferencesService _preferencesService;
    private readonly IOutfitService _outfitService;

    public GetAiGenerationReadiness(
        IPersonalProfileService personalProfileService,
        IAiGenerationPreferencesService preferencesService,
        IOutfitService outfitService)
    {
        _personalProfileService = personalProfileService;
        _preferencesService = preferencesService;
        _outfitService = outfitService;
    }

    public async Task<AiGenerationReadinessResult> ExecuteAsync(Guid outfitId)
    {
        var profile = await _personalProfileService.GetCurrentAsync();
        var preferences = await _preferencesService.GetAsync();
        var outfit = await _outfitService.GetOutfitByIdAsync(outfitId);

        var reasons = new List<string>();

        if (profile == null)
        {
            reasons.Add("还没有填写个人档案。");
        }
        else
        {
            if (string.IsNullOrWhiteSpace(profile.DisplayName))
                reasons.Add("个人档案还缺少昵称。");

            if (!profile.HasAvatarPhoto)
                reasons.Add("请先上传至少一张头像照。");

            if (!profile.HasConsent)
                reasons.Add("请先同意把参考照发送到云端生成。");
        }

        if (string.IsNullOrWhiteSpace(preferences.BaseUrl) || string.IsNullOrWhiteSpace(preferences.Model))
            reasons.Add("AI 图片生成服务还没有配置完成。");

        if (!preferences.HasEncryptedApiKey)
            reasons.Add("还没有保存 API Key。");

        var validClothes = outfit?.OutfitClothes
            .Where(link => link.Clothing != null)
            .Select(link => link.Clothing!)
            .ToList() ?? [];

        if (validClothes.Count < 2)
            reasons.Add("当前搭配至少需要 2 件有效单品。");

        return new AiGenerationReadinessResult(reasons.Count == 0, reasons, profile, preferences);
    }
}
