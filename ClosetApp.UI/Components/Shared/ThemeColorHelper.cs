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
}
