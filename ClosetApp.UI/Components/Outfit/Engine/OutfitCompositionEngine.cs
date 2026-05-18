using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Outfit.Engine;

public class OutfitCompositionEngine
{
    private readonly OutfitRenderMetrics _metrics;

    public OutfitCompositionEngine(OutfitRenderMetrics? metrics = null)
    {
        _metrics = metrics ?? new OutfitRenderMetrics();
    }

    private static LayerRole ResolveLayerRole(global::ClosetApp.Domain.Entities.Clothing c)
    {
        if (c.GarmentType.HasValue)
            return ClothingMappings.GetLayerRole(c.GarmentType.Value);
        return ClothingMappings.GetLayerRole(ClothingMappings.InferGarmentType(c.Type));
    }

    public CompositionMode DetermineMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes)
    {
        if (clothes == null || clothes.Count == 0) return CompositionMode.Solo;

        bool HasRole(LayerRole role) => clothes.Any(c => ResolveLayerRole(c) == role);

        bool hasFullBody = HasRole(LayerRole.FullBody);
        bool hasTop = HasRole(LayerRole.BaseTop) || HasRole(LayerRole.MidLayer);
        bool hasBottom = HasRole(LayerRole.Bottom);

        if (!hasFullBody && !hasTop && !hasBottom) return CompositionMode.Solo;
        if (hasFullBody) return CompositionMode.Dress;
        if (hasTop && hasBottom) return CompositionMode.TopBottom;
        return CompositionMode.Mixed;
    }

    public List<OutfitLayoutItem> CalculateLayout(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        if (clothes == null || clothes.Count == 0) return new List<OutfitLayoutItem>();
        var mode = DetermineMode(clothes);
        return mode switch
        {
            CompositionMode.Dress => DressMode(clothes, cw, ch),
            CompositionMode.TopBottom => TopBottomMode(clothes, cw, ch),
            CompositionMode.Mixed => MixedMode(clothes, cw, ch),
            _ => SoloMode(clothes[0], cw, ch)
        };
    }

    private sealed class OutfitParts
    {
        public global::ClosetApp.Domain.Entities.Clothing? Outer { get; init; }
        public global::ClosetApp.Domain.Entities.Clothing? Mid { get; init; }
        public global::ClosetApp.Domain.Entities.Clothing? Inner { get; init; }
        public global::ClosetApp.Domain.Entities.Clothing? Dress { get; init; }
        public global::ClosetApp.Domain.Entities.Clothing? Bottom { get; init; }
        public global::ClosetApp.Domain.Entities.Clothing? Shoes { get; init; }
        public global::ClosetApp.Domain.Entities.Clothing? Accessory { get; init; }

        public global::ClosetApp.Domain.Entities.Clothing? PrimaryUpper => Outer ?? Mid ?? Inner ?? Dress;
        public global::ClosetApp.Domain.Entities.Clothing? InnerUpper => Outer != null ? Mid ?? Inner : null;
    }

    private static OutfitParts GetParts(IList<global::ClosetApp.Domain.Entities.Clothing> clothes)
    {
        return new OutfitParts
        {
            Outer = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.OuterLayer),
            Mid = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.MidLayer),
            Inner = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.BaseTop),
            Dress = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.FullBody),
            Bottom = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.Bottom),
            Shoes = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.Footwear),
            Accessory = clothes.FirstOrDefault(c => ResolveLayerRole(c) == LayerRole.Accessory)
        };
    }

    private static LayerRole ResolveSoloLayerRole(global::ClosetApp.Domain.Entities.Clothing item)
    {
        if (item.GarmentType.HasValue)
            return ClothingMappings.GetLayerRole(item.GarmentType.Value);
        return ClothingMappings.GetLayerRole(ClothingMappings.InferGarmentType(item.Type));
    }

    private static double CenterX(double canvasWidth, double itemWidth) => (canvasWidth - itemWidth) / 2;

    private List<OutfitLayoutItem> SoloMode(global::ClosetApp.Domain.Entities.Clothing item, double cw, double ch)
    {
        var role = ResolveSoloLayerRole(item);

        var (wRatio, xRatio, yRatio, hRatio) = role switch
        {
            LayerRole.FullBody => (0.82, 0.09, 0.03, 0.65),
            LayerRole.OuterLayer => (0.80, 0.10, 0.02, 0.75),
            LayerRole.BaseTop or LayerRole.MidLayer => (0.74, 0.13, 0.05, 0.85),
            LayerRole.Bottom => (0.72, 0.14, 0.08, 0.80),
            LayerRole.Footwear => (0.58, 0.21, 0.15, 1.45),
            LayerRole.Accessory => (0.38, 0.31, 0.15, 1.0),
            _ => (0.70, 0.15, 0.05, 0.85)
        };

        double w = cw * wRatio;
        double x = cw * xRatio;
        double h = Math.Min(ch * 0.75, w / hRatio);
        double y = (ch - h) / 2;

        return new List<OutfitLayoutItem>
        {
            new()
            {
                Clothing = item,
                X = x, Y = y, Width = w, Height = h,
                ZIndex = 2, Opacity = 1.0
            }
        };
    }

    private List<OutfitLayoutItem> DressMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = GetParts(clothes);

        double upperBudget = parts.Shoes != null ? ch * 0.68 : ch * 0.80;
        double shoesBudget = parts.Shoes != null ? ch * 0.16 : 0;
        double y = ch * 0.03;

        if (parts.Dress != null)
        {
            double w = cw * _metrics.DressWidthRatio;
            double x = CenterX(cw, w);
            double h = Math.Min(upperBudget, w / _metrics.DressHeightRatio);
            items.Add(new() { Clothing = parts.Dress, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
            y += h;
        }

        if (parts.Outer != null)
        {
            double w = cw * 0.66;
            double x = CenterX(cw, w);
            double h = Math.Min(ch * 0.26, w / _metrics.OuterwearHeightRatio);
            items.Add(new() { Clothing = parts.Outer, X = x, Y = ch * 0.02, Width = w, Height = h, ZIndex = 3, Opacity = _metrics.OuterwearOpacity, IsInset = true });
        }

        if (parts.Shoes != null)
        {
            double w = cw * _metrics.ShoesWidthRatio;
            double x = CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = y, Width = w, Height = h, ZIndex = 4, Opacity = 1.0 });
            y += h;
        }

        if (parts.Accessory != null)
        {
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * _metrics.AccessoryRightOffsetRatio;
            double h = w;
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = y - h * 0.5, Width = w, Height = h, ZIndex = 5, Opacity = 1.0, IsInset = true });
        }

        return items;
    }

    private List<OutfitLayoutItem> TopBottomMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = GetParts(clothes);

        double upperBudget = parts.Shoes != null ? ch * 0.40 : ch * 0.46;
        double lowerBudget = parts.Shoes != null ? ch * 0.32 : ch * 0.40;
        double shoesBudget = parts.Shoes != null ? ch * 0.16 : 0;
        double y = ch * 0.03;

        if (parts.PrimaryUpper != null)
        {
            var upper = parts.PrimaryUpper;
            bool isOuterLed = upper == parts.Outer;
            double w = cw * (isOuterLed ? _metrics.OuterwearWidthRatio : _metrics.TopWidthRatio);
            double x = CenterX(cw, w);
            double aspect = isOuterLed ? _metrics.OuterwearHeightRatio : _metrics.TopHeightRatio;
            double h = Math.Min(upperBudget, w / aspect);
            items.Add(new() { Clothing = upper, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = isOuterLed ? _metrics.OuterwearOpacity : 1.0 });

            if (parts.InnerUpper != null)
            {
                double insetW = cw * 0.42;
                double insetH = Math.Min(h * 0.62, insetW / _metrics.TopHeightRatio);
                double insetX = CenterX(cw, insetW);
                double insetY = y + h * 0.30;
                items.Add(new() { Clothing = parts.InnerUpper, X = insetX, Y = insetY, Width = insetW, Height = insetH, ZIndex = 3, Opacity = 1.0, IsInset = true });
            }

            y += h;
        }

        if (parts.Bottom != null)
        {
            double w = cw * _metrics.BottomWidthRatio;
            double x = CenterX(cw, w);
            double h = Math.Min(lowerBudget, w / _metrics.BottomHeightRatio);
            items.Add(new() { Clothing = parts.Bottom, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
            y += h;
        }

        if (parts.Shoes != null)
        {
            double w = cw * _metrics.ShoesWidthRatio;
            double x = CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = y, Width = w, Height = h, ZIndex = 4, Opacity = 1.0 });
            y += h;
        }

        if (parts.Accessory != null)
        {
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * _metrics.AccessoryRightOffsetRatio;
            double h = w;
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = y - h * 0.5, Width = w, Height = h, ZIndex = 5, Opacity = 1.0, IsInset = true });
        }

        return items;
    }

    private List<OutfitLayoutItem> MixedMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = GetParts(clothes);

        double y = ch * 0.04;
        double shoesBudget = parts.Shoes != null ? ch * 0.18 : 0;
        double lowerBudget = parts.Bottom != null ? ch * 0.34 : 0;
        double upperBudget = parts.Bottom != null ? ch * 0.38 : ch * 0.58;

        if (parts.PrimaryUpper != null)
        {
            var upper = parts.PrimaryUpper;
            bool isOuterLed = upper == parts.Outer;
            bool isDress = upper == parts.Dress;
            double w = cw * (isDress ? _metrics.DressWidthRatio : isOuterLed ? _metrics.OuterwearWidthRatio : _metrics.TopWidthRatio);
            double x = CenterX(cw, w);
            double aspect = isDress ? _metrics.DressHeightRatio : isOuterLed ? _metrics.OuterwearHeightRatio : _metrics.TopHeightRatio;
            double h = Math.Min(upperBudget, w / aspect);
            items.Add(new() { Clothing = upper, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = isOuterLed ? _metrics.OuterwearOpacity : 1.0 });

            if (parts.InnerUpper != null)
            {
                double insetW = cw * 0.42;
                double insetH = Math.Min(h * 0.62, insetW / _metrics.TopHeightRatio);
                double insetX = CenterX(cw, insetW);
                double insetY = y + h * 0.30;
                items.Add(new() { Clothing = parts.InnerUpper, X = insetX, Y = insetY, Width = insetW, Height = insetH, ZIndex = 3, Opacity = 1.0, IsInset = true });
            }

            y += h + 6;
        }

        if (parts.Bottom != null)
        {
            double w = cw * _metrics.BottomWidthRatio;
            double x = CenterX(cw, w);
            double h = Math.Min(lowerBudget, w / _metrics.BottomHeightRatio);
            items.Add(new() { Clothing = parts.Bottom, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
            y += h + 4;
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.42;
            double x = CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = y, Width = w, Height = h, ZIndex = 4, Opacity = 1.0 });
            y += h + 4;
        }

        if (parts.Accessory != null)
        {
            double w = cw * 0.28;
            double x = cw * 0.62;
            double h = Math.Min(ch * 0.18, w);
            double accessoryY = Math.Max(ch * 0.06, y - h - 8);
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = accessoryY, Width = w, Height = h, ZIndex = 5, Opacity = 1.0, IsInset = true });
        }

        return items;
    }
}
