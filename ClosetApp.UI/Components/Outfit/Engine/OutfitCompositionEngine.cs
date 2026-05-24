using System.Diagnostics;
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

    private static RenderRole ResolveRenderRole(LayerRole layerRole, CompositionMode mode, bool hasInnerUpper)
    {
        return (layerRole, mode) switch
        {
            (LayerRole.FullBody, _) => RenderRole.Primary,
            (LayerRole.OuterLayer, CompositionMode.Dress) => RenderRole.Overlay,
            (LayerRole.OuterLayer, CompositionMode.TopBottom) when hasInnerUpper => RenderRole.Overlay,
            (LayerRole.OuterLayer, _) => RenderRole.Primary,
            (LayerRole.BaseTop or LayerRole.MidLayer, _) => RenderRole.Primary,
            (LayerRole.Bottom, _) => RenderRole.Bottom,
            (LayerRole.Footwear, _) => RenderRole.Footwear,
            (LayerRole.Accessory, _) => RenderRole.Accessory,
            _ => RenderRole.Primary
        };
    }

    public CompositionMode DetermineMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes)
    {
        if (clothes == null || clothes.Count == 0) return CompositionMode.Solo;

        bool HasRole(LayerRole role) => clothes.Any(c => ResolveLayerRole(c) == role);

        bool hasFullBody = HasRole(LayerRole.FullBody);
        bool hasTop = HasRole(LayerRole.BaseTop) || HasRole(LayerRole.MidLayer);
        bool hasOuter = HasRole(LayerRole.OuterLayer);
        bool hasBottom = HasRole(LayerRole.Bottom);

        if (!hasFullBody && !hasTop && !hasOuter && !hasBottom) return CompositionMode.Solo;
        if (hasFullBody) return CompositionMode.Dress;
        if ((hasTop || hasOuter) && hasBottom) return CompositionMode.TopBottom;
        if (hasTop || hasBottom || hasOuter) return CompositionMode.Mixed;
        return CompositionMode.Solo;
    }

    public List<OutfitLayoutItem> CalculateLayout(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        if (clothes == null || clothes.Count == 0) return new List<OutfitLayoutItem>();

        var mode = DetermineMode(clothes);
        var ctx = RenderContext.Create(clothes, cw, ch, _metrics, mode);

        var items = mode switch
        {
            CompositionMode.Dress => DressMode(ctx),
            CompositionMode.TopBottom => TopBottomMode(ctx),
            CompositionMode.Mixed => MixedMode(ctx),
            _ => SoloMode(ctx)
        };

#if DEBUG
        Debug.WriteLine($"[Engine] Mode={mode}, Items={items.Count}, Canvas={cw:F0}x{ch:F0}");
        foreach (var item in items)
            Debug.WriteLine($"  → {item.Clothing.Name}: Role={item.RenderRole}, Pos=({item.X:F0},{item.Y:F0}), Size={item.Width:F0}x{item.Height:F0}, Z={item.ZIndex}");
#endif

        return items;
    }

    private static LayerRole ResolveSoloLayerRole(global::ClosetApp.Domain.Entities.Clothing item)
    {
        if (item.GarmentType.HasValue)
            return ClothingMappings.GetLayerRole(item.GarmentType.Value);
        return ClothingMappings.GetLayerRole(ClothingMappings.InferGarmentType(item.Type));
    }

    private static double CenterX(double canvasWidth, double itemWidth) => (canvasWidth - itemWidth) / 2;

    private static double CenterInZone(double zoneTop, double zoneHeight, double itemHeight)
    {
        if (zoneHeight <= itemHeight) return zoneTop;
        return zoneTop + ((zoneHeight - itemHeight) / 2);
    }

    private static double AlignToGround(double canvasHeight, double itemHeight, double groundLineRatio)
    {
        return (canvasHeight * groundLineRatio) - itemHeight;
    }

    private List<OutfitLayoutItem> SoloMode(RenderContext ctx)
    {
        var clothes = ctx.Parts.Dress ?? ctx.Parts.Outer ?? ctx.Parts.Mid ?? ctx.Parts.Inner
            ?? ctx.Parts.Bottom ?? ctx.Parts.Shoes ?? ctx.Parts.Accessory;
        if (clothes == null) return new List<OutfitLayoutItem>();

        var role = ResolveSoloLayerRole(clothes);
        double cw = ctx.CanvasWidth, ch = ctx.CanvasHeight;

        var (wRatio, xRatio, yRatio, hRatio) = role switch
        {
            LayerRole.FullBody => (0.86, 0.07, 0.02, 0.65),
            LayerRole.OuterLayer => (0.84, 0.08, 0.01, 0.75),
            LayerRole.BaseTop or LayerRole.MidLayer => (0.72, 0.14, 0.06, 0.85),
            LayerRole.Bottom => (0.68, 0.16, 0.12, 0.80),
            LayerRole.Footwear => (0.40, 0.30, 0.16, 1.45),
            LayerRole.Accessory => (0.24, 0.38, 0.16, 1.0),
            _ => (0.68, 0.16, 0.06, 0.85)
        };

        double w = cw * wRatio;
        double x = cw * xRatio;
        double h = Math.Min(ch * 0.82, w / hRatio);
        double y = role switch
        {
            LayerRole.FullBody => ch * 0.03,
            LayerRole.OuterLayer => ch * 0.02,
            LayerRole.Bottom => ch * 0.10,
            LayerRole.Footwear => ch * 0.62,
            LayerRole.Accessory => ch * 0.10,
            _ => (ch - h) / 2
        };

        var renderRole = role switch
        {
            LayerRole.Footwear => RenderRole.Footwear,
            LayerRole.Accessory => RenderRole.Accessory,
            LayerRole.Bottom => RenderRole.Bottom,
            _ => RenderRole.Primary
        };

        return new List<OutfitLayoutItem>
        {
            new()
            {
                Clothing = clothes,
                X = x, Y = y, Width = w, Height = h,
                ZIndex = 2, Opacity = 1.0, RenderRole = renderRole
            }
        };
    }

    private List<OutfitLayoutItem> DressMode(RenderContext ctx)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = ctx.Parts;
        var m = ctx.Metrics;
        double cw = ctx.CanvasWidth, ch = ctx.CanvasHeight;

        double gap = ch * 0.008;
        double outerBandTop = ch * 0.02;
        double outerBandHeight = parts.Outer != null ? ch * 0.17 : 0;
        double dressBandTop = parts.Outer != null ? outerBandTop + outerBandHeight + gap : ch * 0.015;
        double dressBandHeight = parts.Shoes != null ? ch * 0.70 : ch * 0.80;
        double shoesZoneTop = ch * 0.72;
        double shoesBudget = parts.Shoes != null ? ch * 0.19 : 0;
        double dressBottom = 0;
        double dressX = 0;
        double dressW = 0;

        if (parts.Dress != null)
        {
            double w = cw * 0.80;
            double x = CenterX(cw, w);
            double h = Math.Min(dressBandHeight, w / m.DressHeightRatio);
            double y = CenterInZone(dressBandTop, dressBandHeight, h);
            dressX = x; dressW = w; dressBottom = y + h;
            items.Add(new() { Clothing = parts.Dress, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0, RenderRole = RenderRole.Primary });
        }

        if (parts.Outer != null)
        {
            double w = cw * 0.58;
            double x = CenterX(cw, w);
            double h = Math.Min(outerBandHeight, w / m.OuterwearHeightRatio);
            double y = CenterInZone(outerBandTop, outerBandHeight, h);
            items.Add(new() { Clothing = parts.Outer, X = x, Y = y, Width = w, Height = h, ZIndex = 3, Opacity = m.OuterwearOpacity, RenderRole = ResolveRenderRole(LayerRole.OuterLayer, ctx.Mode, ctx.HasInnerUpper) });
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.42;
            double x = dressW > 0 ? dressX + ((dressW - w) / 2) : CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / m.ShoesHeightRatio);
            var desiredY = dressBottom > 0 ? dressBottom + (ch * 0.002) : shoesZoneTop;
            var maxY = Math.Min(ch - h - 8, AlignToGround(ch, h, 0.86));
            var shoeY = Math.Min(maxY, Math.Max(desiredY, shoesZoneTop));
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = shoeY, Width = w, Height = h, ZIndex = 4, Opacity = 0.98, RenderRole = RenderRole.Footwear });
        }

        if (parts.Accessory != null)
        {
            double w = cw * m.AccessoryWidthRatio;
            double x = cw * 0.69;
            double h = w;
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = ch * 0.15, Width = w, Height = h, ZIndex = 5, Opacity = 0.96, RenderRole = RenderRole.Accessory });
        }

        return items;
    }

    private List<OutfitLayoutItem> TopBottomMode(RenderContext ctx)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = ctx.Parts;
        var m = ctx.Metrics;
        double cw = ctx.CanvasWidth, ch = ctx.CanvasHeight;

        double gap = ch * 0.022;
        double upperAnchorBottom = 0;
        double lowerAnchorBottom = 0;
        double bottomX = 0;
        double bottomW = 0;

        if (parts.PrimaryUpper != null)
        {
            var upper = parts.PrimaryUpper;
            bool isOuterLed = upper == parts.Outer;
            if (isOuterLed && parts.InnerUpper != null)
            {
                double outerW = cw * 0.52;
                double outerH = Math.Min(ch * 0.30, outerW / m.OuterwearHeightRatio);
                double outerX = CenterX(cw, outerW);
                double outerY = ch * 0.04;
                upperAnchorBottom = Math.Max(upperAnchorBottom, outerY + outerH);
                items.Add(new() { Clothing = upper, X = outerX, Y = outerY, Width = outerW, Height = outerH, ZIndex = 3, Opacity = m.OuterwearOpacity, RenderRole = ResolveRenderRole(LayerRole.OuterLayer, ctx.Mode, ctx.HasInnerUpper) });

                double innerW = cw * 0.42;
                double innerH = Math.Min(ch * 0.22, innerW / m.TopHeightRatio);
                double innerX = CenterX(cw, innerW);
                double innerY = ch * 0.13;
                upperAnchorBottom = Math.Max(upperAnchorBottom, innerY + innerH);
                items.Add(new() { Clothing = parts.InnerUpper, X = innerX, Y = innerY, Width = innerW, Height = innerH, ZIndex = 4, Opacity = 0.98, RenderRole = RenderRole.Primary });
            }
            else
            {
                double w = cw * (isOuterLed ? 0.56 : 0.50);
                double x = CenterX(cw, w);
                double aspect = isOuterLed ? m.OuterwearHeightRatio : m.TopHeightRatio;
                double h = Math.Min(ch * 0.32, w / aspect);
                double y = ch * 0.05;
                upperAnchorBottom = y + h;
                items.Add(new() { Clothing = upper, X = x, Y = y, Width = w, Height = h, ZIndex = 3, Opacity = isOuterLed ? m.OuterwearOpacity : 1.0, RenderRole = isOuterLed ? ResolveRenderRole(LayerRole.OuterLayer, ctx.Mode, false) : RenderRole.Primary });
            }
        }

        if (parts.Bottom != null)
        {
            double w = cw * 0.46;
            double x = CenterX(cw, w);
            double h = Math.Min(ch * 0.26, w / m.BottomHeightRatio);
            double lowerZoneY = Math.Max(ch * 0.43, upperAnchorBottom + gap);
            bottomX = x; bottomW = w;
            lowerAnchorBottom = lowerZoneY + h;
            items.Add(new() { Clothing = parts.Bottom, X = x, Y = lowerZoneY, Width = w, Height = h, ZIndex = 2, Opacity = 1.0, RenderRole = RenderRole.Bottom });
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.32;
            double x = bottomW > 0 ? bottomX + ((bottomW - w) / 2) : CenterX(cw, w);
            double h = Math.Min(ch * 0.15, w / m.ShoesHeightRatio);
            var desiredY = lowerAnchorBottom > 0 ? lowerAnchorBottom + (ch * 0.012) : ch * 0.74;
            var maxY = Math.Min(ch - h - 8, AlignToGround(ch, h, 0.86));
            var shoeY = Math.Min(maxY, Math.Max(desiredY, ch * 0.74));
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = shoeY, Width = w, Height = h, ZIndex = 5, Opacity = 0.98, RenderRole = RenderRole.Footwear });
        }

        if (parts.Accessory != null)
        {
            double w = cw * m.AccessoryWidthRatio;
            double x = cw * 0.68;
            double h = w;
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = ch * 0.13, Width = w, Height = h, ZIndex = 5, Opacity = 0.96, RenderRole = RenderRole.Accessory });
        }

        return items;
    }

    private List<OutfitLayoutItem> MixedMode(RenderContext ctx)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = ctx.Parts;
        var m = ctx.Metrics;
        double cw = ctx.CanvasWidth, ch = ctx.CanvasHeight;

        double gap = ch * 0.022;
        double lowerAnchorBottom = 0;
        double primaryAnchorBottom = 0;
        double bottomX = 0;
        double bottomW = 0;
        double primaryX = 0;
        double primaryW = 0;

        if (parts.PrimaryUpper != null)
        {
            var upper = parts.PrimaryUpper;
            bool isOuterLed = upper == parts.Outer;
            bool isDress = upper == parts.Dress;
            if (isOuterLed && parts.InnerUpper != null)
            {
                double outerW = cw * 0.52;
                double outerH = Math.Min(ch * 0.30, outerW / m.OuterwearHeightRatio);
                double outerX = CenterX(cw, outerW);
                double outerY = ch * 0.04;
                items.Add(new() { Clothing = upper, X = outerX, Y = outerY, Width = outerW, Height = outerH, ZIndex = 3, Opacity = m.OuterwearOpacity, RenderRole = ResolveRenderRole(LayerRole.OuterLayer, ctx.Mode, ctx.HasInnerUpper) });

                double innerW = cw * 0.42;
                double innerH = Math.Min(ch * 0.22, innerW / m.TopHeightRatio);
                double innerX = CenterX(cw, innerW);
                double innerY = ch * 0.13;
                primaryX = innerX; primaryW = innerW;
                primaryAnchorBottom = innerY + innerH;
                items.Add(new() { Clothing = parts.InnerUpper, X = innerX, Y = innerY, Width = innerW, Height = innerH, ZIndex = 4, Opacity = 0.98, RenderRole = RenderRole.Primary });
            }
            else
            {
                double w = cw * (isDress ? 0.58 : isOuterLed ? 0.56 : 0.50);
                double x = CenterX(cw, w);
                double aspect = isDress ? m.DressHeightRatio : isOuterLed ? m.OuterwearHeightRatio : m.TopHeightRatio;
                double maxH = parts.Bottom != null ? ch * 0.34 : ch * 0.48;
                double h = Math.Min(maxH, w / aspect);
                double upperZoneY = ch * 0.05;
                primaryX = x; primaryW = w;
                primaryAnchorBottom = upperZoneY + h;
                items.Add(new() { Clothing = upper, X = x, Y = upperZoneY, Width = w, Height = h, ZIndex = 3, Opacity = isOuterLed ? m.OuterwearOpacity : 1.0, RenderRole = ResolveRenderRole(isOuterLed ? LayerRole.OuterLayer : LayerRole.FullBody, ctx.Mode, false) });
            }
        }

        if (parts.Bottom != null)
        {
            double w = cw * 0.46;
            double x = CenterX(cw, w);
            double h = Math.Min(ch * 0.26, w / m.BottomHeightRatio);
            double lowerZoneY = Math.Max(ch * 0.43, primaryAnchorBottom + gap);
            bottomX = x; bottomW = w;
            lowerAnchorBottom = lowerZoneY + h;
            items.Add(new() { Clothing = parts.Bottom, X = x, Y = lowerZoneY, Width = w, Height = h, ZIndex = 2, Opacity = 1.0, RenderRole = RenderRole.Bottom });
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.32;
            double x;
            if (bottomW > 0) x = bottomX + ((bottomW - w) / 2);
            else if (primaryW > 0) x = primaryX + ((primaryW - w) / 2);
            else x = CenterX(cw, w);

            double h = Math.Min(ch * 0.15, w / m.ShoesHeightRatio);
            var anchorBottom = lowerAnchorBottom > 0 ? lowerAnchorBottom : primaryAnchorBottom;
            var desiredY = anchorBottom > 0 ? anchorBottom + (ch * 0.012) : ch * 0.74;
            var maxY = Math.Min(ch - h - 8, AlignToGround(ch, h, 0.86));
            var shoeY = Math.Min(maxY, Math.Max(desiredY, ch * 0.74));
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = shoeY, Width = w, Height = h, ZIndex = 5, Opacity = 0.98, RenderRole = RenderRole.Footwear });
        }

        if (parts.Accessory != null)
        {
            double w = cw * m.AccessoryWidthRatio;
            double x = cw * 0.70;
            double h = Math.Min(ch * 0.18, w);
            double accessoryY = parts.Bottom != null
                ? Math.Max(ch * 0.16, Math.Max(ch * 0.43, primaryAnchorBottom + gap) - h - 10)
                : Math.Max(ch * 0.12, ch * 0.16);
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = accessoryY, Width = w, Height = h, ZIndex = 5, Opacity = 0.96, RenderRole = RenderRole.Accessory });
        }

        return items;
    }
}
