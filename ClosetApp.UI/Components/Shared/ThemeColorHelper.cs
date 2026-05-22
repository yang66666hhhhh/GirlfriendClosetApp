using System.Windows.Media;

namespace ClosetApp.UI.Components.Shared;

public static class ThemeColorHelper
{
    public static Color GetThemeColor(string key, Color fallback)
    {
        if (global::System.Windows.Application.Current?.TryFindResource(key) is SolidColorBrush brush)
            return brush.Color;
        return fallback;
    }

    public static Color Blend(Color a, Color b, double amount)
    {
        byte Lerp(byte x, byte y) => (byte)(x + (y - x) * amount);
        return Color.FromArgb(a.A, Lerp(a.R, b.R), Lerp(a.G, b.G), Lerp(a.B, b.B));
    }

    public static Color ResolveClothingBackdrop(string? colorField)
    {
        var baseColor = GetThemeColor("SurfaceHeroBrush", Color.FromRgb(244, 239, 233));

        if (string.IsNullOrWhiteSpace(colorField))
            return baseColor;

        var c = colorField.ToLowerInvariant();
        var tint = ResolveColorTint(c);
        return tint.HasValue ? Blend(baseColor, tint.Value, 0.45) : baseColor;
    }

    public static Color ResolveOutfitBackdrop(string? season, IEnumerable<string?> clothingColors)
    {
        var baseColor = GetThemeColor("SurfaceHeroBrush", Color.FromRgb(244, 239, 233));

        var firstColor = clothingColors
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.ToLowerInvariant())
            .FirstOrDefault();

        if (firstColor != null)
        {
            var tint = ResolveColorTint(firstColor);
            if (tint.HasValue)
                return Blend(baseColor, tint.Value, 0.45);
        }

        var seasonTint = ResolveSeasonTint(season);
        return seasonTint.HasValue ? Blend(baseColor, seasonTint.Value, 0.4) : baseColor;
    }

    private static Color? ResolveColorTint(string c)
    {
        if (c.Contains("pink") || c.Contains("粉")) return Color.FromRgb(255, 225, 232);
        if (c.Contains("white") || c.Contains("cream") || c.Contains("白") || c.Contains("米")) return Color.FromRgb(252, 250, 244);
        if (c.Contains("blue") || c.Contains("蓝")) return Color.FromRgb(220, 235, 252);
        if (c.Contains("green") || c.Contains("绿")) return Color.FromRgb(225, 245, 230);
        if (c.Contains("yellow") || c.Contains("黄")) return Color.FromRgb(252, 248, 220);
        if (c.Contains("red") || c.Contains("红")) return Color.FromRgb(255, 230, 228);
        if (c.Contains("black") || c.Contains("黑") || c.Contains("gray") || c.Contains("grey") || c.Contains("灰")) return Color.FromRgb(235, 235, 238);
        if (c.Contains("purple") || c.Contains("紫")) return Color.FromRgb(240, 232, 248);
        if (c.Contains("orange") || c.Contains("橙") || c.Contains("棕") || c.Contains("brown")) return Color.FromRgb(250, 238, 225);
        return null;
    }

    private static Color? ResolveSeasonTint(string? season)
    {
        if (string.IsNullOrWhiteSpace(season)) return null;
        return season switch
        {
            "Spring" => Color.FromRgb(255, 242, 235),
            "Summer" => Color.FromRgb(228, 240, 250),
            "Autumn" => Color.FromRgb(250, 240, 225),
            "Winter" => Color.FromRgb(232, 235, 245),
            _ => null
        };
    }

    public static (Color Background, Color Border, Color Foreground) ResolveChipPalette(string chip)
    {
        var baseBg = GetThemeColor("PrimaryLightBrush", Color.FromRgb(250, 232, 237));
        var baseBorder = GetThemeColor("BorderLightBrush", Color.FromRgb(240, 228, 224));
        var baseFg = GetThemeColor("PrimaryBrush", Color.FromRgb(218, 148, 165));

        return chip switch
        {
            "春" or "Spring" => (Blend(baseBg, Color.FromRgb(255, 235, 230), 0.5), Blend(baseBorder, Color.FromRgb(240, 200, 195), 0.3), Blend(baseFg, Color.FromRgb(188, 121, 110), 0.4)),
            "夏" or "Summer" => (Blend(baseBg, Color.FromRgb(230, 248, 245), 0.5), Blend(baseBorder, Color.FromRgb(185, 225, 218), 0.3), Blend(baseFg, Color.FromRgb(92, 145, 136), 0.4)),
            "秋" or "Autumn" => (Blend(baseBg, Color.FromRgb(252, 240, 225), 0.5), Blend(baseBorder, Color.FromRgb(235, 208, 178), 0.3), Blend(baseFg, Color.FromRgb(176, 122, 79), 0.4)),
            "冬" or "Winter" => (Blend(baseBg, Color.FromRgb(235, 238, 248), 0.5), Blend(baseBorder, Color.FromRgb(200, 208, 228), 0.3), Blend(baseFg, Color.FromRgb(110, 121, 153), 0.4)),
            "四季" or "AllSeason" => (Blend(baseBg, Color.FromRgb(240, 236, 250), 0.5), Blend(baseBorder, Color.FromRgb(212, 202, 235), 0.3), Blend(baseFg, Color.FromRgb(126, 108, 170), 0.4)),
            "通勤" or "Work" => (Blend(baseBg, Color.FromRgb(248, 240, 230), 0.5), Blend(baseBorder, Color.FromRgb(225, 208, 188), 0.3), Blend(baseFg, Color.FromRgb(135, 112, 95), 0.4)),
            "约会" or "Date" => (Blend(baseBg, Color.FromRgb(255, 232, 240), 0.4), Blend(baseBorder, Color.FromRgb(242, 195, 212), 0.3), Blend(baseFg, Color.FromRgb(181, 108, 134), 0.3)),
            "出游" or "Travel" => (Blend(baseBg, Color.FromRgb(232, 248, 230), 0.5), Blend(baseBorder, Color.FromRgb(195, 225, 185), 0.3), Blend(baseFg, Color.FromRgb(104, 145, 92), 0.4)),
            "派对" or "Party" => (Blend(baseBg, Color.FromRgb(245, 232, 248), 0.4), Blend(baseBorder, Color.FromRgb(218, 195, 228), 0.3), Blend(baseFg, Color.FromRgb(126, 98, 152), 0.4)),
            "休闲" or "Casual" => (Blend(baseBg, Color.FromRgb(250, 242, 228), 0.5), Blend(baseBorder, Color.FromRgb(230, 215, 185), 0.3), Blend(baseFg, Color.FromRgb(150, 120, 88), 0.4)),
            "上衣" or "Top" => (Blend(baseBg, Color.FromRgb(250, 238, 235), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(170, 110, 100), 0.3)),
            "裤装" or "Bottom" => (Blend(baseBg, Color.FromRgb(238, 242, 250), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(100, 115, 155), 0.3)),
            "连衣裙" or "Dress" => (Blend(baseBg, Color.FromRgb(252, 235, 242), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(175, 95, 125), 0.3)),
            "半裙" or "Skirt" => (Blend(baseBg, Color.FromRgb(248, 236, 245), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(155, 100, 140), 0.3)),
            "外套" or "Outerwear" => (Blend(baseBg, Color.FromRgb(240, 240, 245), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(110, 110, 130), 0.3)),
            "鞋子" or "Shoes" => (Blend(baseBg, Color.FromRgb(245, 238, 230), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(140, 115, 90), 0.3)),
            "配饰" or "Accessory" => (Blend(baseBg, Color.FromRgb(242, 238, 248), 0.4), baseBorder, Blend(baseFg, Color.FromRgb(120, 100, 150), 0.3)),
            _ => (baseBg, baseBorder, baseFg)
        };
    }
}
