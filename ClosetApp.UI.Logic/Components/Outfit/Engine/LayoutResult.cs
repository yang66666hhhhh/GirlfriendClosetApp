using System.Windows;

namespace ClosetApp.UI.Components.Outfit.Engine;

public sealed class LayoutResult
{
    private readonly List<OutfitLayoutItem> _items;

    public IReadOnlyList<OutfitLayoutItem> Items => _items;
    public CompositionMode Mode { get; }
    public OutfitParts Parts { get; }
    public double CanvasWidth { get; }
    public double CanvasHeight { get; }

    public LayoutResult(List<OutfitLayoutItem> items, CompositionMode mode, OutfitParts parts, double cw, double ch)
    {
        _items = items;
        Mode = mode;
        Parts = parts;
        CanvasWidth = cw;
        CanvasHeight = ch;
    }

    public IEnumerable<OutfitLayoutItem> FindByRegion(SemanticRegion region)
        => _items.Where(i => i.SemanticRegion == region);

    public IEnumerable<OutfitLayoutItem> FindByRole(RenderRole role)
        => _items.Where(i => i.RenderRole == role);

    public OutfitLayoutItem? FindPrimary()
        => _items.FirstOrDefault(i => i.SemanticRegion == SemanticRegion.UpperPrimary);

    public Rect? GetRegionBounds(SemanticRegion region)
    {
        var regionItems = FindByRegion(region).ToList();
        if (regionItems.Count == 0) return null;

        double minX = regionItems.Min(i => i.X);
        double minY = regionItems.Min(i => i.Y);
        double maxX = regionItems.Max(i => i.X + i.Width);
        double maxY = regionItems.Max(i => i.Y + i.Height);

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    public Rect? GetPrimaryBounds() => GetRegionBounds(SemanticRegion.UpperPrimary);
}
