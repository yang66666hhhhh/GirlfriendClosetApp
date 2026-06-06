using ClosetApp.UI.Logic.Services;
using Xunit;

namespace ClosetApp.Tests;

public class OutfitCardEffectImageLayoutTests
{
    [Fact]
    public void ResolvePreviewRowHeight_LandscapeImage_DoesNotForceTallMinimum()
    {
        var height = OutfitCardEffectImageLayout.ResolvePreviewRowHeight(1600, 900, 296);

        Assert.InRange(height, 220, 299);
    }

    [Fact]
    public void ResolvePreviewRowHeight_PortraitImage_IsTallerThanLandscapeImage()
    {
        var landscape = OutfitCardEffectImageLayout.ResolvePreviewRowHeight(1600, 900, 296);
        var portrait = OutfitCardEffectImageLayout.ResolvePreviewRowHeight(900, 1600, 296);

        Assert.True(portrait > landscape);
    }

    [Fact]
    public void ResolvePreviewRowHeight_InvalidSize_ReturnsStableFallback()
    {
        var height = OutfitCardEffectImageLayout.ResolvePreviewRowHeight(0, 900, 296);

        Assert.Equal(344, height);
    }
}
