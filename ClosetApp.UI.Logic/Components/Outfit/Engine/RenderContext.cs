namespace ClosetApp.UI.Logic.Components.Outfit.Engine;

public sealed class RenderContext
{
    public required CompositionMode Mode { get; init; }
    public required OutfitParts Parts { get; init; }
    public required double CanvasWidth { get; init; }
    public required double CanvasHeight { get; init; }
    public required OutfitRenderMetrics Metrics { get; init; }

    public double Gap => CanvasHeight * 0.022;
    public bool HasInnerUpper => Parts.InnerUpper != null;

    public static RenderContext Create(
        IList<global::ClosetApp.Domain.Entities.Clothing> clothes,
        double cw, double ch,
        OutfitRenderMetrics metrics,
        CompositionMode mode)
    {
        return new RenderContext
        {
            Mode = mode,
            Parts = OutfitParts.FromClothes(clothes),
            CanvasWidth = cw,
            CanvasHeight = ch,
            Metrics = metrics
        };
    }
}
