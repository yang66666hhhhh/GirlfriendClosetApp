namespace ClosetApp.UI.Components.Outfit.Engine;

public class OutfitRenderMetrics
{
    public double CanvasWidth { get; init; } = 280;
    public double CanvasHeight { get; init; } = 360;

    public double OuterwearWidthRatio { get; init; } = 0.82;
    public double DressWidthRatio { get; init; } = 0.78;
    public double TopWidthRatio { get; init; } = 0.72;
    public double BottomWidthRatio { get; init; } = 0.74;
    public double ShoesWidthRatio { get; init; } = 0.44;
    public double AccessoryWidthRatio { get; init; } = 0.32;

    public double OuterwearHeightRatio { get; init; } = 1.0 / 0.75;
    public double DressHeightRatio { get; init; } = 1.0 / 0.65;
    public double TopHeightRatio { get; init; } = 1.0 / 0.85;
    public double BottomHeightRatio { get; init; } = 1.0 / 0.80;
    public double ShoesHeightRatio { get; init; } = 1.0 / 1.45;

    public double OuterwearOpacity { get; init; } = 0.97;
    public double MainOpacity { get; init; } = 1.0;

    public double ShoesLeftOffsetRatio { get; init; } = 0.12;
    public double AccessoryRightOffsetRatio { get; init; } = 0.58;

    public double MainZoneHeightRatio { get; init; } = 0.60;
    public double ShoesZoneHeightRatio { get; init; } = 0.18;
    public double OuterwearZoneHeightRatio { get; init; } = 0.22;
    public double AccessoryZoneHeightRatio { get; init; } = 0.18;

    public double ShadowBlur { get; init; } = 10;
    public double ShadowDepth { get; init; } = 2;
    public double ShadowOpacity { get; init; } = 0.09;
}
