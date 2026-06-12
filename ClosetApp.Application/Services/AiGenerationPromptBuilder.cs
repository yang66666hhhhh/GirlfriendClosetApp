using ClosetApp.Application.DTOs;
using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Entities;

namespace ClosetApp.Application.Services;

public static class AiGenerationPromptBuilder
{
    public static string BuildOutfitEffectPrompt(
        PersonalProfile profile,
        Outfit outfit,
        GenerateOutfitEffectImageRequest request)
    {
        var clothes = outfit.OutfitClothes
            .Where(link => link.Clothing != null)
            .Select(link => link.Clothing!)
            .ToList();

        var clothingSummary = string.Join("；", clothes.Select(clothing =>
        {
            var garmentName = clothing.GarmentType.HasValue
                ? ClothingMappings.GetDisplayName(clothing.GarmentType.Value)
                : clothing.Type.ToString();

            return $"{clothing.Name}，类型 {garmentName}，颜色 {clothing.Color ?? "未标注"}，品牌 {clothing.Brand ?? "未标注"}";
        }));

        return
            $"请基于提供的同一人物参考图生成一张写实风格的女性穿搭效果图。" +
            $"{BuildProfileSummary(profile)}" +
            $"搭配名称：{outfit.Name}。场景：{request.Scene}。姿态：{request.Pose}。背景：{request.BackgroundStyle}。构图：{request.Framing}。情绪：{request.Mood}。" +
            $"当前搭配单品：{clothingSummary}。" +
            $"{BuildReferencePhotoSummary(profile.AvatarPhotoPath, profile.FullBodyPhotoPath)}" +
            $"请优先保持人物相貌与体态一致，尽量还原颜色、层次与穿搭氛围。不要生成多个人，不要夸张变形，不要额外添加与搭配冲突的服装。";
    }

    public static string BuildProfilePreviewPrompt(PersonalProfileDto profile)
    {
        var previewProfile = new PersonalProfile
        {
            DisplayName = profile.DisplayName,
            HeightCm = profile.HeightCm,
            BodyShape = profile.BodyShape,
            SkinTone = profile.SkinTone,
            HairLength = profile.HairLength,
            HairColor = profile.HairColor,
            FaceFeaturesSummary = profile.FaceFeaturesSummary,
            StyleKeywords = profile.StyleKeywords,
            AvoidKeywords = profile.AvoidKeywords,
            AvatarPhotoPath = profile.AvatarPhotoPath,
            FullBodyPhotoPath = profile.FullBodyPhotoPath
        };

        return
            $"默认生成提示词会从这里出发：" +
            $"请基于提供的同一人物参考图生成一张写实风格的女性穿搭效果图。" +
            $"{BuildProfileSummary(previewProfile)}" +
            $"系统会自动补充你当前选择的搭配单品摘要，以及生成时选择的场景、姿态、背景、构图和情绪。" +
            $"{BuildReferencePhotoSummary(profile.AvatarPhotoPath, profile.FullBodyPhotoPath)}" +
            $"请优先保持人物相貌与体态一致，尽量贴近真实穿搭层次与颜色，不要额外生成冲突服装。";
    }

    private static string BuildProfileSummary(PersonalProfile profile)
    {
        return
            $"人物昵称：{ValueOrFallback(profile.DisplayName, "未填写")}。" +
            $"身高：{ValueOrFallback(profile.HeightCm?.ToString(), "未填写")}cm，身材：{ValueOrFallback(profile.BodyShape, "未填写")}，肤色：{ValueOrFallback(profile.SkinTone, "未填写")}，发型：{ValueOrFallback(profile.HairLength, "未填写")}，发色：{ValueOrFallback(profile.HairColor, "未填写")}，五官特征：{ValueOrFallback(profile.FaceFeaturesSummary, "未填写")}。" +
            $"风格偏好：{ValueOrFallback(profile.StyleKeywords, "未填写")}。避开元素：{ValueOrFallback(profile.AvoidKeywords, "未填写")}。";
    }

    private static string BuildReferencePhotoSummary(string? avatarPhotoPath, string? fullBodyPhotoPath)
    {
        var hasAvatar = !string.IsNullOrWhiteSpace(avatarPhotoPath);
        var hasFullBody = !string.IsNullOrWhiteSpace(fullBodyPhotoPath);

        if (hasAvatar && hasFullBody)
            return "参考图：当前包含上半身照和全身照。";

        if (hasAvatar)
            return "参考图：当前已提供上半身照，全身照暂未提供，生成时会更依赖上半身特征与文字描述。";

        return "参考图：当前还没有上半身照，正式生成前需要先补充。";
    }

    private static string ValueOrFallback(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
