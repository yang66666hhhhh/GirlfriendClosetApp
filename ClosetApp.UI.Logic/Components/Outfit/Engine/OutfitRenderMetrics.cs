namespace ClosetApp.UI.Logic.Components.Outfit.Engine;

public class OutfitRenderMetrics
{
    public double CanvasWidth { get; init; } = 280;
    public double CanvasHeight { get; init; } = 380;

    public double OuterwearWidthRatio { get; init; } = 0.86;
    public double DressWidthRatio { get; init; } = 0.84;
    public double TopWidthRatio { get; init; } = 0.70;
    public double BottomWidthRatio { get; init; } = 0.68;
    public double ShoesWidthRatio { get; init; } = 0.30;
    public double AccessoryWidthRatio { get; init; } = 0.18;

    public double OuterwearHeightRatio { get; init; } = 1.0 / 0.75;
    public double DressHeightRatio { get; init; } = 1.0 / 0.65;
    public double TopHeightRatio { get; init; } = 1.0 / 0.85;
    public double BottomHeightRatio { get; init; } = 1.0 / 0.80;
    public double ShoesHeightRatio { get; init; } = 1.0 / 1.45;

    public double OuterwearOpacity { get; init; } = 0.95;
    public double MainOpacity { get; init; } = 1.0;

    public double ShoesLeftOffsetRatio { get; init; } = 0.12;
    public double AccessoryRightOffsetRatio { get; init; } = 0.58;

    public double MainZoneHeightRatio { get; init; } = 0.60;
    public double ShoesZoneHeightRatio { get; init; } = 0.18;
    public double OuterwearZoneHeightRatio { get; init; } = 0.22;
    public double AccessoryZoneHeightRatio { get; init; } = 0.18;

    public double ShadowBlur { get; init; } = 8;
    public double ShadowDepth { get; init; } = 2;
    public double ShadowOpacity { get; init; } = 0.07;
}
