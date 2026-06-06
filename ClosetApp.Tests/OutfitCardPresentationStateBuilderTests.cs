using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;
using ClosetApp.UI.Logic.Services;
using ClosetApp.UI.Services;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitCardPresentationStateBuilderTests
{
    [Fact]
    public void Build_OutfitFirstWithSucceededImage_StillUsesOutfitPreview()
    {
        var outfit = CreateOutfitWithGeneratedImages(CreateSucceededImage(isPrimary: true));

        var result = OutfitCardPresentationStateBuilder.Build(outfit, OutfitCardDisplayMode.OutfitFirst);

        Assert.Equal(OutfitCardVisualMode.OutfitPreview, result.VisualMode);
        Assert.True(result.HasSucceededEffectImage);
        Assert.False(result.IsFallbackToOutfitPreview);
    }

    [Fact]
    public void Build_EffectImageFirstWithPrimaryImage_UsesPrimaryEffectImage()
    {
        var primary = CreateSucceededImage(isPrimary: true, resultImagePath: "primary.png");
        var secondary = CreateSucceededImage(isPrimary: false, resultImagePath: "secondary.png");
        var outfit = CreateOutfitWithGeneratedImages(primary, secondary);

        var result = OutfitCardPresentationStateBuilder.Build(outfit, OutfitCardDisplayMode.EffectImageFirst);

        Assert.Equal(OutfitCardVisualMode.EffectImage, result.VisualMode);
        Assert.Equal("primary.png", result.EffectImagePath);
        Assert.True(result.IsPrimaryImage);
    }

    [Fact]
    public void Build_EffectImageFirstWithOnlyHistoryImage_UsesFirstSucceededImage()
    {
        var image = CreateSucceededImage(isPrimary: false, resultImagePath: "history.png");
        var outfit = CreateOutfitWithGeneratedImages(image);

        var result = OutfitCardPresentationStateBuilder.Build(outfit, OutfitCardDisplayMode.EffectImageFirst);

        Assert.Equal(OutfitCardVisualMode.EffectImage, result.VisualMode);
        Assert.Equal("history.png", result.EffectImagePath);
        Assert.False(result.IsPrimaryImage);
    }

    [Fact]
    public void Build_EffectImageFirstWithoutSucceededImage_FallsBackToOutfitPreview()
    {
        var outfit = CreateOutfitWithGeneratedImages();

        var result = OutfitCardPresentationStateBuilder.Build(outfit, OutfitCardDisplayMode.EffectImageFirst);

        Assert.Equal(OutfitCardVisualMode.OutfitPreview, result.VisualMode);
        Assert.True(result.IsFallbackToOutfitPreview);
        Assert.Contains("等待效果图", result.HintText);
    }

    [Fact]
    public void Build_EffectImageFirstWithOnlyFailedAttempt_ReturnsFallbackAndFailureHint()
    {
        var failed = new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            Status = "Failed",
            FailureReason = "超时",
            CreatedAt = DateTime.Now
        };
        var outfit = CreateOutfitWithGeneratedImages(failed);

        var result = OutfitCardPresentationStateBuilder.Build(outfit, OutfitCardDisplayMode.EffectImageFirst);

        Assert.Equal(OutfitCardVisualMode.OutfitPreview, result.VisualMode);
        Assert.True(result.IsFallbackToOutfitPreview);
        Assert.True(result.HasFailedAttempt);
        Assert.Contains("生成失败", result.HintText);
    }

    private static Outfit CreateOutfitWithGeneratedImages(params OutfitGeneratedImage[] images)
    {
        var outfit = new Outfit
        {
            Id = Guid.NewGuid(),
            Name = "测试搭配",
            Scene = OutfitScene.Casual,
            Season = Season.Spring
        };

        foreach (var image in images)
        {
            image.OutfitId = outfit.Id;
            outfit.GeneratedImages.Add(image);
        }

        return outfit;
    }

    private static OutfitGeneratedImage CreateSucceededImage(bool isPrimary, string resultImagePath = "result.png")
    {
        return new OutfitGeneratedImage
        {
            Id = Guid.NewGuid(),
            Status = "Succeeded",
            IsPrimary = isPrimary,
            ResultImagePath = resultImagePath,
            Model = "manual-upload",
            CreatedAt = DateTime.Now
        };
    }
}
