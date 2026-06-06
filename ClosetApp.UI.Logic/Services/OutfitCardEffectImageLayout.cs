namespace ClosetApp.UI.Logic.Services;

public static class OutfitCardEffectImageLayout
{
    public static double ResolvePreviewRowHeight(double imageWidth, double imageHeight, double cardWidth)
    {
        if (imageWidth <= 0 || imageHeight <= 0 || cardWidth <= 0 ||
            double.IsNaN(imageWidth) || double.IsNaN(imageHeight) || double.IsNaN(cardWidth))
            return 344;

        const double horizontalImageMargin = 24;
        const double verticalImageMargin = 22;
        var availableImageWidth = Math.Max(1, cardWidth - horizontalImageMargin);
        var fittedHeight = availableImageWidth * imageHeight / imageWidth + verticalImageMargin;

        return Math.Clamp(fittedHeight, 240, 540);
    }
}
