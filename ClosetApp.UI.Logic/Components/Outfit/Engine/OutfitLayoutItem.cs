namespace ClosetApp.UI.Logic.Components.Outfit.Engine;

public class OutfitLayoutItem
{
    public required global::ClosetApp.Domain.Entities.Clothing Clothing { get; init; }
    public required double X { get; init; }
    public required double Y { get; init; }
    public required double Width { get; init; }
    public required double Height { get; init; }
    public required int ZIndex { get; init; }
    public required RenderRole RenderRole { get; init; }
    public SemanticRegion SemanticRegion { get; set; }
    public double Opacity { get; init; } = 1.0;
    public bool IsInset { get; init; }
}
