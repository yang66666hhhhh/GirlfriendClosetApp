using ClosetApp.Domain.Clothing;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Outfit.Engine;

public class OutfitCompositionEngine
{
    private const double DressShoeGroundLineRatio = 0.81;
    private const double SeparateShoeGroundLineRatio = 0.80;
    private const double TopZoneTopRatio = 0.015;
    private const double BottomZoneTopRatio = 0.25;
    private const double ShoesZoneTopRatio = 0.60;

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

    private static double AlignToGround(double canvasHeight, double itemHeight, double groundLineRatio)
    {
        return (canvasHeight * groundLineRatio) - itemHeight;
    }

    private static double AlignToZoneStart(double canvasHeight, double zoneTopRatio)
    {
        return canvasHeight * zoneTopRatio;
    }

    private static double CenterInZone(double zoneTop, double zoneHeight, double itemHeight)
    {
        if (zoneHeight <= itemHeight)
            return zoneTop;

        return zoneTop + ((zoneHeight - itemHeight) / 2);
    }

    private List<OutfitLayoutItem> SoloMode(global::ClosetApp.Domain.Entities.Clothing item, double cw, double ch)
    {
        var role = ResolveSoloLayerRole(item);

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

        double topGap = ch * 0.02;
        double outerBandTop = ch * 0.03;
        double outerBandHeight = parts.Outer != null ? ch * 0.21 : 0;
        double dressBandTop = parts.Outer != null ? outerBandTop + outerBandHeight + topGap : ch * 0.02;
        double dressBandHeight = parts.Shoes != null ? ch * 0.60 : ch * 0.74;
        double shoesZoneTop = ch * 0.76;
        double shoesBudget = parts.Shoes != null ? ch * 0.15 : 0;

        if (parts.Dress != null)
        {
            double w = cw * 0.70;
            double x = CenterX(cw, w);
            double h = Math.Min(dressBandHeight, w / _metrics.DressHeightRatio);
            double y = CenterInZone(dressBandTop, dressBandHeight, h);
            items.Add(new() { Clothing = parts.Dress, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
        }

        if (parts.Outer != null)
        {
            double w = cw * 0.48;
            double x = CenterX(cw, w);
            double h = Math.Min(outerBandHeight, w / _metrics.OuterwearHeightRatio);
            double y = CenterInZone(outerBandTop, outerBandHeight, h);
            items.Add(new() { Clothing = parts.Outer, X = x, Y = y, Width = w, Height = h, ZIndex = 3, Opacity = _metrics.OuterwearOpacity });
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.30;
            double x = CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            var shoeY = Math.Max(shoesZoneTop, AlignToGround(ch, h, DressShoeGroundLineRatio));
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = shoeY, Width = w, Height = h, ZIndex = 4, Opacity = 0.98 });
        }

        if (parts.Accessory != null)
        {
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * 0.69;
            double h = w;
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = ch * 0.15, Width = w, Height = h, ZIndex = 5, Opacity = 0.96 });
        }

        return items;
    }

    private List<OutfitLayoutItem> TopBottomMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = GetParts(clothes);

        double gap = ch * 0.02;
        double outerBandTop = ch * 0.03;
        double outerBandHeight = ch * 0.21;
        double innerBandTop = outerBandTop + outerBandHeight + gap;
        double innerBandHeight = ch * 0.16;
        double upperBandTop = ch * 0.04;
        double upperBandHeight = ch * 0.34;
        double lowerY = ch * 0.48;
        double lowerBudget = ch * 0.24;
        double shoesZoneTop = ch * 0.77;
        double shoesBudget = parts.Shoes != null ? ch * 0.14 : 0;

        if (parts.PrimaryUpper != null)
        {
            var upper = parts.PrimaryUpper;
            bool isOuterLed = upper == parts.Outer;
            if (isOuterLed && parts.InnerUpper != null)
            {
                double outerW = cw * 0.52;
                double outerH = Math.Min(outerBandHeight, outerW / _metrics.OuterwearHeightRatio);
                double outerX = CenterX(cw, outerW);
                double outerY = CenterInZone(outerBandTop, outerBandHeight, outerH);
                items.Add(new() { Clothing = upper, X = outerX, Y = outerY, Width = outerW, Height = outerH, ZIndex = 3, Opacity = _metrics.OuterwearOpacity });

                double innerW = cw * 0.46;
                double innerH = Math.Min(innerBandHeight, innerW / _metrics.TopHeightRatio);
                double innerX = CenterX(cw, innerW);
                double innerY = CenterInZone(innerBandTop, innerBandHeight, innerH);
                items.Add(new() { Clothing = parts.InnerUpper, X = innerX, Y = innerY, Width = innerW, Height = innerH, ZIndex = 4, Opacity = 0.98 });
            }
            else
            {
                double w = cw * (isOuterLed ? 0.60 : 0.56);
                double x = CenterX(cw, w);
                double aspect = isOuterLed ? _metrics.OuterwearHeightRatio : _metrics.TopHeightRatio;
                double h = Math.Min(upperBandHeight, w / aspect);
                double upperZoneY = CenterInZone(upperBandTop, upperBandHeight, h);
                items.Add(new() { Clothing = upper, X = x, Y = upperZoneY, Width = w, Height = h, ZIndex = 3, Opacity = isOuterLed ? _metrics.OuterwearOpacity : 1.0 });
            }
        }

        if (parts.Bottom != null)
        {
            double w = cw * 0.56;
            double x = CenterX(cw, w);
            double h = Math.Min(lowerBudget, w / _metrics.BottomHeightRatio);
            double lowerZoneY = CenterInZone(lowerY, lowerBudget, h);
            items.Add(new() { Clothing = parts.Bottom, X = x, Y = lowerZoneY, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.32;
            double x = CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            var shoeY = Math.Max(shoesZoneTop, AlignToGround(ch, h, SeparateShoeGroundLineRatio));
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = shoeY, Width = w, Height = h, ZIndex = 5, Opacity = 0.98 });
        }

        if (parts.Accessory != null)
        {
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * 0.68;
            double h = w;
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = ch * 0.13, Width = w, Height = h, ZIndex = 5, Opacity = 0.96 });
        }

        return items;
    }

    private List<OutfitLayoutItem> MixedMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();
        var parts = GetParts(clothes);

        double gap = ch * 0.02;
        double outerBandTop = ch * 0.03;
        double outerBandHeight = ch * 0.21;
        double innerBandTop = outerBandTop + outerBandHeight + gap;
        double innerBandHeight = ch * 0.16;
        double upperBandTop = ch * 0.04;
        double upperBandHeight = parts.Bottom != null ? ch * 0.34 : ch * 0.60;
        double lowerY = parts.Bottom != null ? ch * 0.48 : 0;
        double lowerBudget = parts.Bottom != null ? ch * 0.24 : 0;
        double shoesZoneTop = parts.Bottom != null ? ch * 0.77 : ch * 0.76;
        double shoesBudget = parts.Shoes != null ? ch * 0.14 : 0;

        if (parts.PrimaryUpper != null)
        {
            var upper = parts.PrimaryUpper;
            bool isOuterLed = upper == parts.Outer;
            bool isDress = upper == parts.Dress;
            if (isOuterLed && parts.InnerUpper != null)
            {
                double outerW = cw * 0.52;
                double outerH = Math.Min(outerBandHeight, outerW / _metrics.OuterwearHeightRatio);
                double outerX = CenterX(cw, outerW);
                double outerY = CenterInZone(outerBandTop, outerBandHeight, outerH);
                items.Add(new() { Clothing = upper, X = outerX, Y = outerY, Width = outerW, Height = outerH, ZIndex = 3, Opacity = _metrics.OuterwearOpacity });

                double innerW = cw * 0.46;
                double innerH = Math.Min(innerBandHeight, innerW / _metrics.TopHeightRatio);
                double innerX = CenterX(cw, innerW);
                double innerY = CenterInZone(innerBandTop, innerBandHeight, innerH);
                items.Add(new() { Clothing = parts.InnerUpper, X = innerX, Y = innerY, Width = innerW, Height = innerH, ZIndex = 4, Opacity = 0.98 });
            }
            else
            {
                double w = cw * (isDress ? 0.68 : isOuterLed ? 0.60 : 0.56);
                double x = CenterX(cw, w);
                double aspect = isDress ? _metrics.DressHeightRatio : isOuterLed ? _metrics.OuterwearHeightRatio : _metrics.TopHeightRatio;
                double h = Math.Min(upperBandHeight, w / aspect);
                double upperZoneY = CenterInZone(upperBandTop, upperBandHeight, h);
                items.Add(new() { Clothing = upper, X = x, Y = upperZoneY, Width = w, Height = h, ZIndex = 3, Opacity = isOuterLed ? _metrics.OuterwearOpacity : 1.0 });
            }
        }

        if (parts.Bottom != null)
        {
            double w = cw * 0.56;
            double x = CenterX(cw, w);
            double h = Math.Min(lowerBudget, w / _metrics.BottomHeightRatio);
            double lowerZoneY = CenterInZone(lowerY, lowerBudget, h);
            items.Add(new() { Clothing = parts.Bottom, X = x, Y = lowerZoneY, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
        }

        if (parts.Shoes != null)
        {
            double w = cw * 0.32;
            double x = CenterX(cw, w);
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            var shoeY = Math.Max(shoesZoneTop, AlignToGround(ch, h, SeparateShoeGroundLineRatio));
            items.Add(new() { Clothing = parts.Shoes, X = x, Y = shoeY, Width = w, Height = h, ZIndex = 5, Opacity = 0.98 });
        }

        if (parts.Accessory != null)
        {
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * 0.70;
            double h = Math.Min(ch * 0.18, w);
            double accessoryY = parts.Bottom != null
                ? Math.Max(ch * 0.12, lowerY - h - 8)
                : Math.Max(ch * 0.12, upperBandTop + ch * 0.06);
            items.Add(new() { Clothing = parts.Accessory, X = x, Y = accessoryY, Width = w, Height = h, ZIndex = 5, Opacity = 0.96 });
        }

        return items;
    }
}
