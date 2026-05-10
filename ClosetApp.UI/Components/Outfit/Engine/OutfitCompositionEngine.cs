using System.Windows.Media;
using System.Windows.Media.Imaging;
using ClosetApp.Domain.Entities;
using ClosetApp.Domain.Enums;

namespace ClosetApp.UI.Components.Outfit.Engine;

public class OutfitCompositionEngine
{
    private readonly OutfitRenderMetrics _metrics;

    public OutfitCompositionEngine(OutfitRenderMetrics? metrics = null)
    {
        _metrics = metrics ?? new OutfitRenderMetrics();
    }

    public CompositionMode DetermineMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes)
    {
        if (clothes == null || clothes.Count == 0) return CompositionMode.Solo;
        var hasDress = clothes.Any(c => c.Type == ClothingType.Dress);
        var hasTop = clothes.Any(c => c.Type is ClothingType.Top or ClothingType.Outerwear);
        var hasBottom = clothes.Any(c => c.Type is ClothingType.Bottom or ClothingType.Skirt);
        if (!hasDress && !hasTop && !hasBottom) return CompositionMode.Solo;
        if (hasDress) return CompositionMode.Dress;
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
            _ => SoloMode(clothes[0], cw, ch)
        };
    }

    private List<OutfitLayoutItem> SoloMode(global::ClosetApp.Domain.Entities.Clothing item, double cw, double ch)
    {
        var (wRatio, xRatio, yStart, hRatio) = item.Type switch
        {
            ClothingType.Dress => (0.82, 0.09, 0.03, 0.65),
            ClothingType.Outerwear => (0.80, 0.10, 0.02, 0.75),
            ClothingType.Top => (0.74, 0.13, 0.05, 0.85),
            ClothingType.Bottom or ClothingType.Skirt => (0.72, 0.14, 0.08, 0.80),
            ClothingType.Shoes => (0.58, 0.21, 0.15, 1.45),
            ClothingType.Accessory => (0.38, 0.31, 0.15, 1.0),
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

        bool hasOuter = clothes.Any(c => c.Type == ClothingType.Outerwear);
        bool hasShoes = clothes.Any(c => c.Type == ClothingType.Shoes);
        bool hasAcc = clothes.Any(c => c.Type == ClothingType.Accessory);

        double mainBudget = hasShoes ? ch * 0.60 : ch * 0.75;
        double shoesBudget = hasShoes ? ch * 0.18 : 0;
        double outerBudget = hasOuter ? ch * 0.22 : 0;

        double y = outerBudget;

        var dress = clothes.FirstOrDefault(c => c.Type == ClothingType.Dress);
        if (dress != null)
        {
            double w = cw * _metrics.DressWidthRatio;
            double x = (cw - w) / 2;
            double h = Math.Min(mainBudget, w / _metrics.DressHeightRatio);
            items.Add(new() { Clothing = dress, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
            y += h;
        }

        if (hasShoes)
        {
            var shoes = clothes.First(c => c.Type == ClothingType.Shoes);
            double w = cw * _metrics.ShoesWidthRatio;
            double x = cw * _metrics.ShoesLeftOffsetRatio;
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            items.Add(new() { Clothing = shoes, X = x, Y = y, Width = w, Height = h, ZIndex = 3, Opacity = 1.0 });
            y += h;
        }

        if (hasAcc)
        {
            var acc = clothes.First(c => c.Type == ClothingType.Accessory);
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * _metrics.AccessoryRightOffsetRatio;
            double h = w;
            items.Add(new() { Clothing = acc, X = x, Y = y - h * 0.5, Width = w, Height = h, ZIndex = 4, Opacity = 1.0 });
        }

        if (hasOuter)
        {
            var outer = clothes.First(c => c.Type == ClothingType.Outerwear);
            double w = cw * _metrics.OuterwearWidthRatio;
            double x = (cw - w) / 2;
            double h = outerBudget + 10;
            items.Add(new() { Clothing = outer, X = x, Y = 0, Width = w, Height = h, ZIndex = 1, Opacity = 0.97 });
        }

        return items;
    }

    private List<OutfitLayoutItem> TopBottomMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();

        bool hasOuter = clothes.Any(c => c.Type == ClothingType.Outerwear);
        bool hasShoes = clothes.Any(c => c.Type == ClothingType.Shoes);
        bool hasAcc = clothes.Any(c => c.Type == ClothingType.Accessory);

        double outerBudget = hasOuter ? ch * 0.22 : 0;
        double mainBudget = hasShoes ? ch * 0.56 : ch * 0.70;
        double shoesBudget = hasShoes ? ch * 0.18 : 0;

        double y = outerBudget;

        var top = clothes.FirstOrDefault(c => c.Type is ClothingType.Top or ClothingType.Outerwear);
        if (top != null)
        {
            double w = cw * _metrics.TopWidthRatio;
            double x = (cw - w) / 2;
            double h = Math.Min(mainBudget * 0.45, w / _metrics.TopHeightRatio);
            items.Add(new() { Clothing = top, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
            y += h;
        }

        var bottom = clothes.FirstOrDefault(c => c.Type is ClothingType.Bottom or ClothingType.Skirt);
        if (bottom != null)
        {
            double w = cw * _metrics.BottomWidthRatio;
            double x = cw * 0.08;
            double h = Math.Min(mainBudget * 0.55, w / _metrics.BottomHeightRatio);
            items.Add(new() { Clothing = bottom, X = x, Y = y, Width = w, Height = h, ZIndex = 2, Opacity = 1.0 });
            y += h;
        }

        if (hasShoes)
        {
            var shoes = clothes.First(c => c.Type == ClothingType.Shoes);
            double w = cw * _metrics.ShoesWidthRatio;
            double x = cw * _metrics.ShoesLeftOffsetRatio;
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            items.Add(new() { Clothing = shoes, X = x, Y = y, Width = w, Height = h, ZIndex = 3, Opacity = 1.0 });
            y += h;
        }

        if (hasAcc)
        {
            var acc = clothes.First(c => c.Type == ClothingType.Accessory);
            double w = cw * _metrics.AccessoryWidthRatio;
            double x = cw * _metrics.AccessoryRightOffsetRatio;
            double h = w;
            items.Add(new() { Clothing = acc, X = x, Y = y - h * 0.5, Width = w, Height = h, ZIndex = 4, Opacity = 1.0 });
        }

        if (hasOuter)
        {
            var outer = clothes.First(c => c.Type == ClothingType.Outerwear);
            double w = cw * _metrics.OuterwearWidthRatio;
            double x = (cw - w) / 2;
            double h = outerBudget + 10;
            items.Add(new() { Clothing = outer, X = x, Y = 0, Width = w, Height = h, ZIndex = 1, Opacity = 0.97 });
        }

        return items;
    }

    private List<OutfitLayoutItem> MixedMode(IList<global::ClosetApp.Domain.Entities.Clothing> clothes, double cw, double ch)
    {
        var items = new List<OutfitLayoutItem>();

        var main = clothes.OrderByDescending(c => c.Type switch
        {
            ClothingType.Dress => 6,
            ClothingType.Outerwear => 5,
            ClothingType.Top => 4,
            ClothingType.Bottom or ClothingType.Skirt => 3,
            ClothingType.Shoes => 2,
            ClothingType.Accessory => 1,
            _ => 0
        }).First();

        bool hasShoes = clothes.Any(c => c.Type == ClothingType.Shoes);
        bool hasAcc = clothes.Any(c => c.Type == ClothingType.Accessory);

        double mainBudget = ch * 0.55;
        double shoesBudget = hasShoes ? ch * 0.20 : 0;
        double accBudget = ch * 0.18;

        double mainW = cw * 0.74;
        double mainX = (cw - mainW) / 2;
        double mainY = 0.02 * ch;
        double aspect = main.Type == ClothingType.Dress ? _metrics.DressHeightRatio :
            main.Type is ClothingType.Bottom or ClothingType.Skirt ? _metrics.BottomHeightRatio : _metrics.TopHeightRatio;
        double mainH = Math.Min(mainBudget, mainW / aspect);
        int mainZ = main.Type == ClothingType.Outerwear ? 1 : 2;

        items.Add(new() { Clothing = main, X = mainX, Y = mainY, Width = mainW, Height = mainH, ZIndex = mainZ, Opacity = 1.0 });

        double currentY = mainY + mainH + 6;
        int accZ = 3;

        if (hasShoes)
        {
            var shoes = clothes.First(c => c.Type == ClothingType.Shoes);
            double w = cw * 0.42;
            double x = cw * 0.10;
            double h = Math.Min(shoesBudget, w / _metrics.ShoesHeightRatio);
            items.Add(new() { Clothing = shoes, X = x, Y = currentY, Width = w, Height = h, ZIndex = accZ++, Opacity = 1.0 });
            currentY += h + 4;
        }

        if (hasAcc)
        {
            var acc = clothes.First(c => c.Type == ClothingType.Accessory);
            double w = cw * 0.28;
            double x = cw * 0.62;
            double h = Math.Min(accBudget, w);
            items.Add(new() { Clothing = acc, X = x, Y = currentY - h, Width = w, Height = h, ZIndex = accZ++, Opacity = 1.0 });
        }

        return items;
    }
}
